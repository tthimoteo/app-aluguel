# 03 — Modelo de Dados PostgreSQL

Banco **PostgreSQL 16** (Supabase). Estratégia multi-tenant: **banco único, schema único, coluna `tenant_id`** em todas as tabelas de negócio, reforçada por **RLS** (ver [06](06-Estrategia-Multi-Tenant.md)).

## 1. Interpretação do domínio

- **`tenant`** é a unidade de isolamento (uma por conta contratada). O campo `TenantId` citado na especificação corresponde a esta coluna, presente em todas as tabelas de negócio.
- **`cliente`** é o titular da conta dentro do tenant (Pessoa Física ou Jurídica). Possui `plano_id` e `status` (Trial/Ativa/Suspensa...).
- **`usuario`** pertence a um `cliente` (operadores/admin da conta).
- **`assinatura`** materializa a cobrança recorrente do `cliente` no Mercado Pago.
- **`contrato`** vincula `imovel` a `inquilino`; **`recebimento`** é a parcela mensal do aluguel.
- **`nota_fiscal_servico`** (NFS-e) referencia um `recebimento`; arquivos ficam no Supabase Storage e seus metadados em `documento_fiscal`.

## 2. Convenções

| Item | Padrão |
|---|---|
| PK | `uuid` default `gen_random_uuid()` |
| Datas | `timestamptz` (UTC) |
| Dinheiro | `numeric(14,2)` |
| Textos livres | `text`; e-mails em `citext` |
| Soft delete | coluna `deleted_at timestamptz null` |
| Auditoria de linha | `created_at`, `updated_at`, `created_by`, `updated_by` |
| Enums | `varchar` + `CHECK` (portável e fácil de evoluir) |
| Nomes | `snake_case`, tabelas no singular |

## 3. Extensões e schema

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;   -- gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS citext;     -- e-mails case-insensitive

CREATE SCHEMA IF NOT EXISTS app;
SET search_path TO app, public;
```

## 4. Catálogo global (não multi-tenant)

```sql
-- Planos disponíveis (Trial, Básico, Intermediário, Avançado, Pro)
CREATE TABLE app.plano (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    codigo         varchar(20)  NOT NULL UNIQUE,          -- TRIAL, BASICO, INTERMEDIARIO, AVANCADO, PRO
    nome           varchar(60)  NOT NULL,
    max_imoveis    int          NULL,                     -- NULL = ilimitado (Pro)
    max_usuarios   int          NULL,
    permite_nfse   boolean      NOT NULL DEFAULT false,
    trial_dias     int          NOT NULL DEFAULT 0,
    valor_mensal   numeric(14,2) NULL,                    -- NULL = sob consulta (Pro)
    ativo          boolean      NOT NULL DEFAULT true,
    created_at     timestamptz  NOT NULL DEFAULT now(),
    updated_at     timestamptz  NOT NULL DEFAULT now()
);

INSERT INTO app.plano (codigo, nome, max_imoveis, max_usuarios, permite_nfse, trial_dias, valor_mensal) VALUES
 ('TRIAL',         'Trial',         1,   1,   false, 7,  0),
 ('BASICO',        'Básico',        3,   1,   false, 0,  49.99),
 ('INTERMEDIARIO', 'Intermediário', 5,   3,   true,  0,  149.99),
 ('AVANCADO',      'Avançado',      10,  5,   true,  0,  249.99),
 ('PRO',           'Pro',           NULL,NULL,true,  0,  NULL);
```

## 5. Tenant

```sql
CREATE TABLE app.tenant (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    nome         varchar(150) NOT NULL,
    subdominio   varchar(63)  NOT NULL UNIQUE,
    ativo        boolean      NOT NULL DEFAULT true,
    created_at   timestamptz  NOT NULL DEFAULT now(),
    updated_at   timestamptz  NOT NULL DEFAULT now()
);
```

## 6. Clientes

```sql
CREATE TABLE app.cliente (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id          uuid NOT NULL REFERENCES app.tenant(id),
    tipo_pessoa        varchar(2)  NOT NULL CHECK (tipo_pessoa IN ('PF','PJ')),
    plano_id           uuid NULL REFERENCES app.plano(id),
    status             varchar(20) NOT NULL DEFAULT 'Trial'
                       CHECK (status IN ('Trial','PendentePagamento','Ativa','Suspensa','Cancelada')),

    -- Pessoa Física
    nome               varchar(150) NULL,
    cpf                varchar(11)  NULL,
    data_nascimento    date         NULL,

    -- Pessoa Jurídica
    razao_social       varchar(150) NULL,
    nome_fantasia      varchar(150) NULL,
    cnpj               varchar(14)  NULL,
    inscricao_municipal varchar(30) NULL,
    cnae               varchar(10)  NULL,

    -- Contato
    telefone           varchar(20)  NULL,
    email              citext       NULL,

    -- Endereço completo
    logradouro         varchar(150) NULL,
    numero             varchar(15)  NULL,
    complemento        varchar(60)  NULL,
    bairro             varchar(80)  NULL,
    cidade             varchar(80)  NULL,
    uf                 char(2)      NULL,
    cep                varchar(8)   NULL,

    created_at         timestamptz NOT NULL DEFAULT now(),
    updated_at         timestamptz NOT NULL DEFAULT now(),
    created_by         uuid NULL,
    updated_by         uuid NULL,
    deleted_at         timestamptz NULL,

    CONSTRAINT ck_cliente_pf CHECK (tipo_pessoa <> 'PF' OR (nome IS NOT NULL AND cpf IS NOT NULL)),
    CONSTRAINT ck_cliente_pj CHECK (tipo_pessoa <> 'PJ' OR (razao_social IS NOT NULL AND cnpj IS NOT NULL))
);

CREATE UNIQUE INDEX ux_cliente_cpf  ON app.cliente(tenant_id, cpf)  WHERE cpf  IS NOT NULL AND deleted_at IS NULL;
CREATE UNIQUE INDEX ux_cliente_cnpj ON app.cliente(tenant_id, cnpj) WHERE cnpj IS NOT NULL AND deleted_at IS NULL;
CREATE INDEX ix_cliente_tenant ON app.cliente(tenant_id);
```

## 7. Usuários

```sql
CREATE TABLE app.usuario (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id     uuid NOT NULL REFERENCES app.tenant(id),
    cliente_id    uuid NOT NULL REFERENCES app.cliente(id),
    perfil        varchar(20) NOT NULL DEFAULT 'Operador'
                  CHECK (perfil IN ('Admin','Operador','Financeiro','Leitura')),
    nome          varchar(150) NOT NULL,
    cpf           varchar(11)  NULL,
    email         citext       NOT NULL,
    -- Identidade externa (Supabase Auth). Senha NÃO é persistida aqui (ver doc 08).
    auth_user_id  uuid NULL,
    ativo         boolean NOT NULL DEFAULT true,
    ultimo_login  timestamptz NULL,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now(),
    deleted_at    timestamptz NULL
);

CREATE UNIQUE INDEX ux_usuario_email ON app.usuario(tenant_id, email) WHERE deleted_at IS NULL;
CREATE INDEX ix_usuario_cliente ON app.usuario(cliente_id);
```

## 8. Assinaturas, pagamentos e auditoria de assinatura

```sql
CREATE TABLE app.assinatura (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                   uuid NOT NULL REFERENCES app.tenant(id),
    cliente_id                  uuid NOT NULL REFERENCES app.cliente(id),
    plano_id                    uuid NOT NULL REFERENCES app.plano(id),
    status                      varchar(20) NOT NULL DEFAULT 'Trial'
                                CHECK (status IN ('Trial','PendentePagamento','Ativa','Suspensa','Cancelada')),
    data_inicio                 timestamptz NOT NULL DEFAULT now(),
    data_fim_trial              timestamptz NULL,
    proxima_cobranca            timestamptz NULL,
    inicio_tolerancia           timestamptz NULL,   -- marca início dos 7 dias de tolerância
    cancelada_em                timestamptz NULL,
    fim_ciclo_pago              timestamptz NULL,   -- até quando permanece ativa após cancelar
    valor_mensal                numeric(14,2) NOT NULL DEFAULT 0,
    mercadopago_subscription_id varchar(60) NULL,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    updated_at                  timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ux_assinatura_cliente_ativa
    ON app.assinatura(cliente_id)
    WHERE status IN ('Trial','PendentePagamento','Ativa');   -- 1 assinatura vigente por cliente
CREATE INDEX ix_assinatura_tenant ON app.assinatura(tenant_id);
CREATE INDEX ix_assinatura_proxima_cobranca ON app.assinatura(proxima_cobranca) WHERE status = 'Ativa';

CREATE TABLE app.pagamento_plano (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                uuid NOT NULL REFERENCES app.tenant(id),
    assinatura_id            uuid NOT NULL REFERENCES app.assinatura(id),
    valor                    numeric(14,2) NOT NULL,
    data_vencimento          timestamptz NOT NULL,
    data_pagamento           timestamptz NULL,
    status                   varchar(20) NOT NULL DEFAULT 'Pendente'
                             CHECK (status IN ('Pendente','Pago','Falhou','Estornado')),
    metodo_pagamento         varchar(20) NULL CHECK (metodo_pagamento IN ('PIX','Cartao','Boleto')),
    mercadopago_payment_id   varchar(60) NULL,
    observacao               text NULL,
    created_at               timestamptz NOT NULL DEFAULT now(),
    updated_at               timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ux_pagamento_mp_id ON app.pagamento_plano(mercadopago_payment_id) WHERE mercadopago_payment_id IS NOT NULL;
CREATE INDEX ix_pagamento_assinatura ON app.pagamento_plano(assinatura_id);

-- Trilha de auditoria específica de assinaturas
CREATE TABLE app.auditoria_assinatura (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id      uuid NOT NULL REFERENCES app.tenant(id),
    assinatura_id  uuid NULL REFERENCES app.assinatura(id),
    cliente_id     uuid NOT NULL REFERENCES app.cliente(id),
    usuario_id     uuid NULL REFERENCES app.usuario(id),
    evento         varchar(40) NOT NULL
                   CHECK (evento IN ('InicioTrial','Contratacao','Renovacao','Pagamento',
                                     'FalhaPagamento','Suspensao','Reativacao','Upgrade','Downgrade','Cancelamento')),
    data_hora      timestamptz NOT NULL DEFAULT now(),
    ip             inet NULL,
    plano_anterior varchar(20) NULL,
    novo_plano     varchar(20) NULL,
    valor          numeric(14,2) NULL,
    descricao      text NULL
);

CREATE INDEX ix_auditoria_assinatura_cliente ON app.auditoria_assinatura(cliente_id, data_hora DESC);
```

## 9. Imóveis, inquilinos, contratos e recebimentos

```sql
CREATE TABLE app.imovel (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id      uuid NOT NULL REFERENCES app.tenant(id),
    cliente_id     uuid NOT NULL REFERENCES app.cliente(id),
    codigo         varchar(30) NOT NULL,
    tipo           varchar(20) NOT NULL CHECK (tipo IN ('Residencial','Comercial')),
    logradouro     varchar(150) NOT NULL,
    numero         varchar(15)  NULL,
    complemento    varchar(60)  NULL,
    bairro         varchar(80)  NULL,
    cidade         varchar(80)  NULL,
    uf             char(2)      NULL,
    cep            varchar(8)   NULL,
    valor_aluguel  numeric(14,2) NOT NULL DEFAULT 0,
    status         varchar(20) NOT NULL DEFAULT 'Disponivel'
                   CHECK (status IN ('Disponivel','Alugado','Manutencao')),
    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now(),
    deleted_at     timestamptz NULL
);
CREATE UNIQUE INDEX ux_imovel_codigo ON app.imovel(tenant_id, codigo) WHERE deleted_at IS NULL;
CREATE INDEX ix_imovel_cliente ON app.imovel(cliente_id);

CREATE TABLE app.inquilino (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id    uuid NOT NULL REFERENCES app.tenant(id),
    cliente_id   uuid NOT NULL REFERENCES app.cliente(id),
    tipo_pessoa  varchar(2) NOT NULL CHECK (tipo_pessoa IN ('PF','PJ')),
    nome         varchar(150) NOT NULL,
    documento    varchar(14)  NOT NULL,   -- CPF ou CNPJ
    telefone     varchar(20)  NULL,
    email        citext       NULL,
    created_at   timestamptz NOT NULL DEFAULT now(),
    updated_at   timestamptz NOT NULL DEFAULT now(),
    deleted_at   timestamptz NULL
);
CREATE UNIQUE INDEX ux_inquilino_doc ON app.inquilino(tenant_id, documento) WHERE deleted_at IS NULL;
CREATE INDEX ix_inquilino_cliente ON app.inquilino(cliente_id);

CREATE TABLE app.contrato (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       uuid NOT NULL REFERENCES app.tenant(id),
    cliente_id      uuid NOT NULL REFERENCES app.cliente(id),
    imovel_id       uuid NOT NULL REFERENCES app.imovel(id),
    inquilino_id    uuid NOT NULL REFERENCES app.inquilino(id),
    valor_aluguel   numeric(14,2) NOT NULL,
    dia_vencimento  int NOT NULL CHECK (dia_vencimento BETWEEN 1 AND 28),
    data_inicio     date NOT NULL,
    data_fim        date NULL,
    status          varchar(20) NOT NULL DEFAULT 'Ativo'
                    CHECK (status IN ('Ativo','Encerrado','Rescindido')),
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_contrato_imovel ON app.contrato(imovel_id);
CREATE INDEX ix_contrato_inquilino ON app.contrato(inquilino_id);

CREATE TABLE app.recebimento (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id      uuid NOT NULL REFERENCES app.tenant(id),
    cliente_id     uuid NOT NULL REFERENCES app.cliente(id),
    contrato_id    uuid NOT NULL REFERENCES app.contrato(id),
    competencia    char(7) NOT NULL,                -- 'MM/AAAA'
    valor          numeric(14,2) NOT NULL,
    data_vencimento date NOT NULL,
    data_pagamento  date NULL,
    status         varchar(20) NOT NULL DEFAULT 'Pendente'
                   CHECK (status IN ('Pendente','Pago','Atrasado','Cancelado')),
    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_recebimento_competencia ON app.recebimento(contrato_id, competencia);
CREATE INDEX ix_recebimento_status ON app.recebimento(tenant_id, status);
```

## 10. Módulo fiscal (NFS-e, certificado, documentos)

```sql
CREATE TABLE app.certificado_digital (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id         uuid NOT NULL REFERENCES app.tenant(id),
    cliente_id        uuid NOT NULL REFERENCES app.cliente(id),
    storage_path      text NOT NULL,                 -- caminho do PFX no Supabase Storage
    thumbprint        varchar(80) NOT NULL,
    validade          timestamptz NOT NULL,
    senha_cifrada     bytea NOT NULL,                -- senha do PFX criptografada (nunca em texto puro)
    ativo             boolean NOT NULL DEFAULT true,
    created_at        timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_certificado_cliente ON app.certificado_digital(cliente_id) WHERE ativo;

CREATE TABLE app.nota_fiscal_servico (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id          uuid NOT NULL REFERENCES app.tenant(id),
    cliente_id         uuid NOT NULL REFERENCES app.cliente(id),
    recebimento_id     uuid NULL REFERENCES app.recebimento(id),
    numero_rps         bigint NULL,
    serie_rps          varchar(5) NULL,
    numero_nfse        bigint NULL,
    codigo_verificacao varchar(50) NULL,
    protocolo          varchar(60) NULL,
    valor_servico      numeric(14,2) NOT NULL,
    valor_iss          numeric(14,2) NULL,
    status             varchar(20) NOT NULL DEFAULT 'Pendente'
                       CHECK (status IN ('Pendente','Processando','Autorizada','Rejeitada','Cancelada')),
    motivo_rejeicao    text NULL,
    motivo_cancelamento text NULL,
    emitida_em         timestamptz NULL,
    cancelada_em       timestamptz NULL,
    created_at         timestamptz NOT NULL DEFAULT now(),
    updated_at         timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_nfse_rps ON app.nota_fiscal_servico(tenant_id, serie_rps, numero_rps)
    WHERE numero_rps IS NOT NULL;
CREATE INDEX ix_nfse_cliente_status ON app.nota_fiscal_servico(cliente_id, status);

-- Metadados dos arquivos fiscais (XML/PDF) guardados no Supabase Storage
CREATE TABLE app.documento_fiscal (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id      uuid NOT NULL REFERENCES app.tenant(id),
    nfse_id        uuid NOT NULL REFERENCES app.nota_fiscal_servico(id),
    tipo           varchar(10) NOT NULL CHECK (tipo IN ('XML','PDF','RPS')),
    storage_path   text NOT NULL,
    content_hash   varchar(64) NULL,       -- SHA-256 p/ integridade
    tamanho_bytes  bigint NULL,
    created_at     timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_documento_fiscal_nfse ON app.documento_fiscal(nfse_id);
```

## 11. Auditoria genérica, Outbox e Inbox

```sql
-- Trilha de auditoria genérica (mudanças de qualquer entidade sensível)
CREATE TABLE app.audit_log (
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tenant_id    uuid NULL,
    entidade     varchar(60) NOT NULL,
    entidade_id  uuid NULL,
    acao         varchar(10) NOT NULL CHECK (acao IN ('Insert','Update','Delete')),
    usuario_id   uuid NULL,
    ip           inet NULL,
    dados_antes  jsonb NULL,
    dados_depois jsonb NULL,
    data_hora    timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_audit_log_entidade ON app.audit_log(tenant_id, entidade, entidade_id);

-- Outbox: eventos de domínio a publicar de forma confiável
CREATE TABLE app.outbox_message (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id      uuid NULL,
    tipo           varchar(120) NOT NULL,
    conteudo       jsonb NOT NULL,
    ocorrido_em    timestamptz NOT NULL DEFAULT now(),
    processado_em  timestamptz NULL,
    tentativas     int NOT NULL DEFAULT 0,
    erro           text NULL
);
CREATE INDEX ix_outbox_pendente ON app.outbox_message(ocorrido_em) WHERE processado_em IS NULL;

-- Inbox: idempotência de webhooks externos (Mercado Pago)
CREATE TABLE app.inbox_message (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    origem        varchar(30) NOT NULL,          -- 'MercadoPago'
    chave_externa varchar(120) NOT NULL,         -- id do evento/pagamento
    recebido_em   timestamptz NOT NULL DEFAULT now(),
    processado_em timestamptz NULL,
    payload       jsonb NOT NULL,
    CONSTRAINT ux_inbox_origem_chave UNIQUE (origem, chave_externa)
);
```

## 12. Gatilho de `updated_at`

```sql
CREATE OR REPLACE FUNCTION app.set_updated_at()
RETURNS trigger AS $$
BEGIN
    NEW.updated_at := now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Exemplo (repetir para cada tabela com updated_at):
CREATE TRIGGER trg_cliente_updated
    BEFORE UPDATE ON app.cliente
    FOR EACH ROW EXECUTE FUNCTION app.set_updated_at();
```

## 13. Row Level Security (resumo)

Habilitada em todas as tabelas com `tenant_id`. Detalhes e políticas completas em [06 — Multi-tenant](06-Estrategia-Multi-Tenant.md).

```sql
ALTER TABLE app.cliente ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_cliente ON app.cliente
    USING (tenant_id = current_setting('app.tenant_id', true)::uuid);
-- (idem para usuario, assinatura, imovel, inquilino, contrato, recebimento,
--  nota_fiscal_servico, certificado_digital, documento_fiscal, auditoria_assinatura ...)
```

Próximo: [04 — ERD completo](04-ERD.md).
