# 03 — Modelo de Dados PostgreSQL

Banco **PostgreSQL** (Supabase). Multi-tenant: **banco único, schema `app`, coluna `tenant_id`** em todas as tabelas de negócio + **RLS** ([06](06-Estrategia-Multi-Tenant.md)). Autenticação com **ASP.NET Core Identity** (tabelas no schema `identity`).

## 1. Convenções

| Item | Padrão |
|---|---|
| PK | `uuid` default `gen_random_uuid()` |
| Datas | `timestamptz` (UTC); competência em `char(7)` = `MM/AAAA` |
| Dinheiro | `numeric(14,2)`; percentuais `numeric(6,3)` |
| Textos | `text`; e-mails `citext` |
| Soft delete | `deleted_at`/`Status=Inativo` (dados nunca excluídos no cancelamento) |
| Auditoria | `created_at`, `updated_at`, `created_by`, `updated_by` + `audit_log` |
| Enums | `varchar` + `CHECK` |
| Nomes | `snake_case`, singular |

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;
CREATE SCHEMA IF NOT EXISTS app;
CREATE SCHEMA IF NOT EXISTS identity;   -- tabelas do ASP.NET Identity
SET search_path TO app, public;
```

## 2. Catálogo (global) e Tenant

```sql
CREATE TABLE app.plano (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    codigo        varchar(20) NOT NULL UNIQUE,   -- TRIAL, BASICO, INTERMEDIARIO, AVANCADO, PRO
    nome          varchar(60) NOT NULL,
    max_imoveis   int  NULL,                      -- NULL = ilimitado (Pro)
    max_usuarios  int  NULL,
    permite_nfse  boolean NOT NULL DEFAULT false,
    trial_dias    int  NOT NULL DEFAULT 0,
    valor_mensal  numeric(14,2) NULL,             -- NULL = sob consulta (Pro)
    ativo         boolean NOT NULL DEFAULT true,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now()
);

INSERT INTO app.plano (codigo, nome, max_imoveis, max_usuarios, permite_nfse, trial_dias, valor_mensal) VALUES
 ('TRIAL',         'Trial',         1,   1,   false, 7, 0),
 ('BASICO',        'Básico',        3,   1,   false, 0, 49.99),
 ('INTERMEDIARIO', 'Intermediário', 5,   3,   true,  0, 149.99),
 ('AVANCADO',      'Avançado',      10,  5,   true,  0, 249.99),
 ('PRO',           'Pro',           NULL,NULL,true,  0, NULL);

CREATE TABLE app.tenant (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome       varchar(150) NOT NULL,
    subdominio varchar(63) NOT NULL UNIQUE,
    ativo      boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
```

## 3. Cliente (locador / emitente da NFS-e)

```sql
CREATE TABLE app.cliente (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           uuid NOT NULL REFERENCES app.tenant(id),
    tipo_pessoa         varchar(2)  NOT NULL CHECK (tipo_pessoa IN ('PF','PJ')),
    plano_id            uuid NULL REFERENCES app.plano(id),
    status              varchar(20) NOT NULL DEFAULT 'Trial'
                        CHECK (status IN ('Trial','PendentePagamento','Ativa','Suspensa','Cancelada')),
    -- PF
    nome                varchar(150) NULL,
    cpf                 varchar(11)  NULL,
    data_nascimento     date NULL,
    -- PJ
    razao_social        varchar(150) NULL,
    nome_fantasia       varchar(150) NULL,
    cnpj                varchar(14)  NULL,
    inscricao_municipal varchar(30)  NULL,
    cnae_principal      varchar(10)  NULL,
    -- contato + endereço
    telefone            varchar(20) NULL,
    email               citext NULL,
    logradouro varchar(150), numero varchar(15), complemento varchar(60),
    bairro varchar(80), cidade varchar(80), uf char(2), cep varchar(8),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    created_by uuid, updated_by uuid,
    deleted_at timestamptz NULL,
    CONSTRAINT ck_cliente_pf CHECK (tipo_pessoa <> 'PF' OR (nome IS NOT NULL AND cpf IS NOT NULL)),
    CONSTRAINT ck_cliente_pj CHECK (tipo_pessoa <> 'PJ' OR (razao_social IS NOT NULL AND cnpj IS NOT NULL))
);
CREATE UNIQUE INDEX ux_cliente_cpf  ON app.cliente(tenant_id, cpf)  WHERE cpf  IS NOT NULL AND deleted_at IS NULL;
CREATE UNIQUE INDEX ux_cliente_cnpj ON app.cliente(tenant_id, cnpj) WHERE cnpj IS NOT NULL AND deleted_at IS NULL;
```

## 4. Usuário (ASP.NET Identity estendido)

O **ASP.NET Core Identity** cria `identity.asp_net_users`, `asp_net_roles`, `asp_net_user_roles`, `asp_net_user_claims`, `asp_net_user_tokens` (para JWT/refresh/MFA). A entidade **`AppUser : IdentityUser<Guid>`** é estendida com colunas de negócio (o "Usuário do cliente" da §6). A **senha** é o `password_hash` do Identity — **nunca** em texto puro.

```sql
-- Identidade (e-mail/senha/CPF) única no tenant. Perfil e status por cliente: app.usuario_cliente.
-- Colunas de negócio em identity.asp_net_users (AppUser):
--   tenant_id  uuid NOT NULL
--   cliente_id uuid NULL          -- último/contexto (JWT); canônico é usuario_cliente
--   nome       varchar(150) NOT NULL
--   cpf        varchar(11)  NULL
--   telefone   varchar(20)  NULL
--   perfil     varchar(20)  NOT NULL CHECK (perfil IN ('Gestor','Analista','Administrador'))
--   status     varchar(20)  NOT NULL DEFAULT 'Ativo' CHECK (status IN ('Ativo','Inativo','Bloqueado'))
--   ultimo_login timestamptz NULL

CREATE UNIQUE INDEX ux_usuario_cpf_tenant ON identity.asp_net_users(tenant_id, cpf) WHERE cpf IS NOT NULL;

CREATE TABLE app.usuario_cliente (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id  uuid NOT NULL REFERENCES app.tenant(id),
    usuario_id uuid NOT NULL REFERENCES identity.asp_net_users(id),
    cliente_id uuid NOT NULL REFERENCES app.cliente(id),
    perfil     varchar(20) NOT NULL CHECK (perfil IN ('Gestor','Analista')),
    status     varchar(20) NOT NULL DEFAULT 'Ativo'
               CHECK (status IN ('Ativo','Inativo','Bloqueado')),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (cliente_id, usuario_id)
);
-- CASO 1: o mesmo CPF não se cadastra duas vezes no mesmo cliente
-- (CPF único na identidade + unique (cliente_id, usuario_id)).
CREATE INDEX ix_usuario_cliente_usuario ON app.usuario_cliente(usuario_id);
```

> Para PF, o primeiro usuário coincide com o cliente e recebe perfil `Gestor` (§6). `AdminSistema` = usuário administrador do app (Lucrare) — ver [08](08-Estrategia-Autenticacao.md).

## 5. Assinaturas, pagamentos de plano e auditoria de assinatura

```sql
CREATE TABLE app.assinatura (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES app.tenant(id),
    cliente_id uuid NOT NULL REFERENCES app.cliente(id),
    plano_id uuid NOT NULL REFERENCES app.plano(id),
    status varchar(20) NOT NULL DEFAULT 'Trial'
        CHECK (status IN ('Trial','PendentePagamento','Ativa','Suspensa','Cancelada')),
    data_inicio timestamptz NOT NULL DEFAULT now(),
    data_fim_trial timestamptz NULL,
    proxima_cobranca timestamptz NULL,
    inicio_tolerancia timestamptz NULL,
    cancelada_em timestamptz NULL,
    fim_ciclo_pago timestamptz NULL,
    valor_mensal numeric(14,2) NOT NULL DEFAULT 0,
    mercadopago_subscription_id varchar(60) NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_assinatura_cliente_vigente ON app.assinatura(cliente_id)
    WHERE status IN ('Trial','PendentePagamento','Ativa');
CREATE INDEX ix_assinatura_proxima_cobranca ON app.assinatura(proxima_cobranca) WHERE status='Ativa';

CREATE TABLE app.pagamento_plano (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES app.tenant(id),
    assinatura_id uuid NOT NULL REFERENCES app.assinatura(id),
    valor numeric(14,2) NOT NULL,
    data_vencimento timestamptz NOT NULL,
    data_pagamento timestamptz NULL,
    status varchar(20) NOT NULL DEFAULT 'Pendente'
        CHECK (status IN ('Pendente','Pago','Falhou','Estornado')),
    metodo_pagamento varchar(20) NULL CHECK (metodo_pagamento IN ('PIX','Cartao')),
    mercadopago_payment_id varchar(60) NULL,
    observacao text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_pagamento_mp_id ON app.pagamento_plano(mercadopago_payment_id) WHERE mercadopago_payment_id IS NOT NULL;

CREATE TABLE app.auditoria_assinatura (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES app.tenant(id),
    assinatura_id uuid NULL REFERENCES app.assinatura(id),
    cliente_id uuid NOT NULL REFERENCES app.cliente(id),
    usuario_id uuid NULL,
    evento varchar(40) NOT NULL
        CHECK (evento IN ('InicioTrial','Contratacao','Renovacao','Pagamento','FalhaPagamento',
                          'Suspensao','Reativacao','Upgrade','Downgrade','Cancelamento')),
    data_hora timestamptz NOT NULL DEFAULT now(),
    ip inet NULL,
    plano_anterior varchar(20) NULL,
    novo_plano varchar(20) NULL,
    valor numeric(14,2) NULL,
    descricao text NULL
);
CREATE INDEX ix_auditoria_assinatura_cliente ON app.auditoria_assinatura(cliente_id, data_hora DESC);
```

## 6. Imóveis, inquilinos e contratos

```sql
CREATE TABLE app.imovel (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES app.tenant(id),
    cliente_id uuid NOT NULL REFERENCES app.cliente(id),
    nome varchar(150) NOT NULL,
    tipo varchar(20) NOT NULL CHECK (tipo IN ('Residencial','Comercial','Galpao','Sala','Outro')),
    logradouro varchar(150), numero varchar(15), complemento varchar(60),
    bairro varchar(80), cidade varchar(80), uf char(2), cep varchar(8),
    numero_iptu varchar(30) NULL,
    numero_matricula varchar(30) NULL,
    status varchar(10) NOT NULL DEFAULT 'Ativo' CHECK (status IN ('Ativo','Inativo')),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz NULL
);
CREATE INDEX ix_imovel_cliente ON app.imovel(cliente_id);

CREATE TABLE app.inquilino (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES app.tenant(id),
    cliente_id uuid NOT NULL REFERENCES app.cliente(id),
    tipo_pessoa varchar(2) NOT NULL CHECK (tipo_pessoa IN ('PF','PJ')),
    nome varchar(150) NOT NULL,          -- nome ou razão social
    documento varchar(14) NOT NULL,      -- CPF ou CNPJ
    inscricao_municipal varchar(30) NULL,
    telefone varchar(20) NULL,
    email citext NULL,
    logradouro varchar(150), numero varchar(15), complemento varchar(60),
    bairro varchar(80), cidade varchar(80), uf char(2), cep varchar(8),
    status varchar(10) NOT NULL DEFAULT 'Ativo' CHECK (status IN ('Ativo','Inativo')),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz NULL
);
CREATE INDEX ix_inquilino_cliente ON app.inquilino(cliente_id);

CREATE TABLE app.contrato (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES app.tenant(id),
    cliente_id uuid NOT NULL REFERENCES app.cliente(id),
    imovel_id uuid NOT NULL REFERENCES app.imovel(id),
    inquilino_id uuid NOT NULL REFERENCES app.inquilino(id),
    numero_contrato varchar(40) NOT NULL,
    status varchar(12) NOT NULL DEFAULT 'Ativo' CHECK (status IN ('Ativo','Encerrado','Cancelado')),
    data_inicio date NOT NULL,
    data_fim_prevista date NULL,
    dia_vencimento int NOT NULL CHECK (dia_vencimento BETWEEN 1 AND 31),
    valor_aluguel numeric(14,2) NOT NULL,
    juros_atraso_pct numeric(6,3) NULL,   -- % ao mês
    multa_atraso_pct numeric(6,3) NULL,   -- %
    anexo_path text NULL,                  -- bucket 'contracts'
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
-- CASO 3: apenas um contrato Ativo por imóvel (⇒ um inquilino por vez)
CREATE UNIQUE INDEX ux_contrato_imovel_ativo ON app.contrato(imovel_id) WHERE status='Ativo';
CREATE INDEX ix_contrato_inquilino ON app.contrato(inquilino_id);
```

## 7. Faturamento / NFS-e

Cada **faturamento** corresponde a uma **NFS-e** de uma competência (§11). Emitente = cliente (locador); destinatário = inquilino.

```sql
CREATE TABLE app.nota_fiscal_servico (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES app.tenant(id),
    cliente_id uuid NOT NULL REFERENCES app.cliente(id),
    imovel_id uuid NOT NULL REFERENCES app.imovel(id),
    contrato_id uuid NULL REFERENCES app.contrato(id),
    inquilino_id uuid NULL REFERENCES app.inquilino(id),
    competencia char(7) NOT NULL,                    -- 'MM/AAAA'
    numero bigint NULL,
    serie varchar(5) NULL,
    chave_acesso varchar(50) NULL,
    status varchar(25) NOT NULL DEFAULT 'Rascunho'
        CHECK (status IN ('Rascunho','EmProcessamento','Emitida','Rejeitada','CancelamentoSolicitado','Cancelada')),
    valor_servico numeric(14,2) NOT NULL,            -- aluguel
    desconto numeric(14,2) NOT NULL DEFAULT 0,
    multa numeric(14,2) NOT NULL DEFAULT 0,
    juros numeric(14,2) NOT NULL DEFAULT 0,
    valor_faturado numeric(14,2) NOT NULL,           -- valor final registrado no histórico
    data_emissao timestamptz NULL,
    usuario_emissor_id uuid NULL,                    -- quem solicitou (§11)
    solicitado_em timestamptz NULL,
    motivo_rejeicao text NULL,
    -- cancelamento (§12)
    motivo_cancelamento text NULL,
    protocolo_cancelamento varchar(60) NULL,
    data_cancelamento timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
-- CASO 6/7/8: só 1 faturamento não-cancelado por imóvel/competência
CREATE UNIQUE INDEX ux_nfse_competencia_ativa
    ON app.nota_fiscal_servico(imovel_id, competencia)
    WHERE status <> 'Cancelada';
CREATE INDEX ix_nfse_cliente_status ON app.nota_fiscal_servico(cliente_id, status);

-- Arquivos fiscais (XML, PDF, XML do evento de cancelamento) no Supabase Storage
CREATE TABLE app.documento_fiscal (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES app.tenant(id),
    nfse_id uuid NOT NULL REFERENCES app.nota_fiscal_servico(id),
    tipo varchar(20) NOT NULL CHECK (tipo IN ('XML','PDF','XMLCancelamento')),
    storage_path text NOT NULL,          -- buckets nfse-xml / nfse-pdf
    content_hash varchar(64) NULL,
    tamanho_bytes bigint NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_documento_fiscal_nfse ON app.documento_fiscal(nfse_id);
```

## 8. Financeiro: pagamentos de aluguel e despesas

```sql
-- Registro de pagamento de aluguel (§12) — vinculado opcionalmente a uma NFS-e
CREATE TABLE app.pagamento (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES app.tenant(id),
    cliente_id uuid NOT NULL REFERENCES app.cliente(id),
    imovel_id uuid NOT NULL REFERENCES app.imovel(id),
    contrato_id uuid NULL REFERENCES app.contrato(id),
    nfse_id uuid NULL REFERENCES app.nota_fiscal_servico(id),
    competencia char(7) NOT NULL,
    valor_pago numeric(14,2) NOT NULL,
    data_pagamento date NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);
-- CASO 6: pagamento não pode ser repetido para a mesma competência do imóvel
CREATE UNIQUE INDEX ux_pagamento_competencia ON app.pagamento(imovel_id, competencia);

-- Despesas: IPTU e outras (§12/§13 Contas a Pagar)
CREATE TABLE app.despesa (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES app.tenant(id),
    cliente_id uuid NOT NULL REFERENCES app.cliente(id),
    imovel_id uuid NOT NULL REFERENCES app.imovel(id),
    tipo varchar(10) NOT NULL CHECK (tipo IN ('IPTU','Outra')),
    descricao text NULL,
    categoria varchar(40) NULL,
    fornecedor varchar(150) NULL,
    competencia char(7) NOT NULL,
    valor numeric(14,2) NOT NULL,
    data_pagamento date NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_despesa_imovel ON app.despesa(imovel_id, competencia);
```

## 9. Certificado digital

```sql
CREATE TABLE app.certificado_digital (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES app.tenant(id),
    cliente_id uuid NOT NULL REFERENCES app.cliente(id),
    storage_path text NOT NULL,          -- bucket 'certificates'
    thumbprint varchar(80) NOT NULL,
    validade timestamptz NOT NULL,
    senha_cifrada bytea NOT NULL,        -- senha do PFX criptografada
    ativo boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_certificado_cliente ON app.certificado_digital(cliente_id) WHERE ativo;
```

## 10. Auditoria geral, Outbox e Inbox

```sql
-- AuditLog (§13): valores antes/depois de qualquer alteração; eventos: login, emissão,
-- cancelamento, pagamento, despesas, alteração cadastral.
CREATE TABLE app.audit_log (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tenant_id uuid NULL,
    usuario_id uuid NULL,
    acao varchar(40) NOT NULL,           -- Login, Emissao, Cancelamento, Pagamento, Despesa, AlteracaoCadastral...
    tabela varchar(60) NULL,
    registro_id uuid NULL,
    valores_antes jsonb NULL,
    valores_depois jsonb NULL,
    data_hora timestamptz NOT NULL DEFAULT now(),
    ip inet NULL
);
CREATE INDEX ix_audit_log_registro ON app.audit_log(tenant_id, tabela, registro_id);

CREATE TABLE app.outbox_message (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NULL,
    tipo varchar(120) NOT NULL,
    conteudo jsonb NOT NULL,
    ocorrido_em timestamptz NOT NULL DEFAULT now(),
    processado_em timestamptz NULL,
    tentativas int NOT NULL DEFAULT 0,
    erro text NULL
);
CREATE INDEX ix_outbox_pendente ON app.outbox_message(ocorrido_em) WHERE processado_em IS NULL;

CREATE TABLE app.inbox_message (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    origem varchar(30) NOT NULL,         -- 'MercadoPago'
    chave_externa varchar(120) NOT NULL,
    recebido_em timestamptz NOT NULL DEFAULT now(),
    processado_em timestamptz NULL,
    payload jsonb NOT NULL,
    CONSTRAINT ux_inbox UNIQUE (origem, chave_externa)
);
```

## 11. Trigger de `updated_at` e RLS

```sql
CREATE OR REPLACE FUNCTION app.set_updated_at() RETURNS trigger AS $$
BEGIN NEW.updated_at := now(); RETURN NEW; END; $$ LANGUAGE plpgsql;
-- aplicar BEFORE UPDATE em cada tabela com updated_at.

ALTER TABLE app.cliente ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_cliente ON app.cliente
    USING (tenant_id = current_setting('app.tenant_id', true)::uuid)
    WITH CHECK (tenant_id = current_setting('app.tenant_id', true)::uuid);
-- idem: imovel, inquilino, contrato, nota_fiscal_servico, documento_fiscal,
--       pagamento, despesa, certificado_digital, assinatura, pagamento_plano,
--       auditoria_assinatura. Detalhes em 06.
```

## 12. Rastreabilidade dos critérios de aceite (§18)

| Regra | Onde é garantida |
|---|---|
| CASO 1 — CPF único por cliente | `ux_usuario_cpf_tenant` + `usuario_cliente(cliente_id, usuario_id)` |
| CASO 2 — downgrade x imóveis | validação de aplicação (doc 06) |
| CASO 3 — 1 contrato ativo por imóvel | `ux_contrato_imovel_ativo` |
| CASO 4 — plano sem NFS-e | `plano.permite_nfse` + policy |
| CASO 5 — suspensa bloqueia Home | guarda de assinatura (doc 06/08) |
| CASO 6 — pagamento único por competência | `ux_pagamento_competencia` |
| CASO 7/8 — 1 NFS-e não-cancelada por competência | `ux_nfse_competencia_ativa` |

Próximo: [04 — ERD](04-ERD.md).
