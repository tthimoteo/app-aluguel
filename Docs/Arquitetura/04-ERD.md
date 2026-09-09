# 04 — ERD (Entity Relationship Diagram)

ERD completo, alinhado ao modelo da especificação (§15) e ao [03 — Modelo PostgreSQL](03-Modelo-PostgreSQL.md).

## 1. Resumo da especificação (§15)

```text
Cliente ├─ Imóvel (N) ├─ Usuário (N) └─ Certificado (1)
Imóvel    └─ Contrato (N)
Inquilino └─ Contrato (N)
Contrato  ├─ NFS-e (N)
NFS-e (1:1) Pagamentos
AuditLog
```

## 2. Diagrama de domínio

```mermaid
erDiagram
    TENANT ||--o{ CLIENTE : possui
    PLANO  ||--o{ CLIENTE : "plano atual"
    PLANO  ||--o{ ASSINATURA : referencia

    CLIENTE ||--o{ USUARIO_CLIENTE : "vincula (N)"
    USUARIO ||--o{ USUARIO_CLIENTE : "atua em (N)"
    CLIENTE ||--o{ IMOVEL : "cadastra (N)"
    CLIENTE ||--o{ INQUILINO : "cadastra"
    CLIENTE ||--o| CERTIFICADO_DIGITAL : "certificado (1 ativo)"
    CLIENTE ||--o{ ASSINATURA : "assina"

    ASSINATURA ||--o{ PAGAMENTO_PLANO : "gera"
    ASSINATURA ||--o{ AUDITORIA_ASSINATURA : "audita"

    IMOVEL ||--o{ CONTRATO : "objeto de (N)"
    INQUILINO ||--o{ CONTRATO : "parte de (N)"
    CONTRATO ||--o{ NOTA_FISCAL_SERVICO : "fatura (N)"

    IMOVEL ||--o{ NOTA_FISCAL_SERVICO : "referente a"
    IMOVEL ||--o{ PAGAMENTO : "recebe"
    IMOVEL ||--o{ DESPESA : "IPTU/outras"

    NOTA_FISCAL_SERVICO ||--o| PAGAMENTO : "1:1 (opcional)"
    NOTA_FISCAL_SERVICO ||--o{ DOCUMENTO_FISCAL : "XML/PDF/evento"

    TENANT {
        uuid id PK
        varchar nome
        varchar subdominio UK
    }
    PLANO {
        uuid id PK
        varchar codigo UK
        int max_imoveis
        int max_usuarios
        boolean permite_nfse
        numeric valor_mensal
    }
    CLIENTE {
        uuid id PK
        uuid tenant_id FK
        varchar tipo_pessoa
        uuid plano_id FK
        varchar status
        varchar cpf
        varchar cnpj
        varchar inscricao_municipal
        varchar cnae_principal
    }
    USUARIO {
        uuid id PK
        uuid tenant_id FK
        varchar nome
        varchar cpf UK
        citext email
        varchar status
    }
    USUARIO_CLIENTE {
        uuid id PK
        uuid usuario_id FK
        uuid cliente_id FK
        varchar perfil
        varchar status
    }
    ASSINATURA {
        uuid id PK
        uuid cliente_id FK
        uuid plano_id FK
        varchar status
        timestamptz data_fim_trial
        timestamptz proxima_cobranca
        varchar mercadopago_subscription_id
    }
    PAGAMENTO_PLANO {
        uuid id PK
        uuid assinatura_id FK
        numeric valor
        varchar status
        varchar metodo_pagamento
        varchar mercadopago_payment_id
    }
    AUDITORIA_ASSINATURA {
        uuid id PK
        uuid cliente_id FK
        varchar evento
        varchar plano_anterior
        varchar novo_plano
        inet ip
    }
    CERTIFICADO_DIGITAL {
        uuid id PK
        uuid cliente_id FK
        text storage_path
        varchar thumbprint
        timestamptz validade
        bytea senha_cifrada
    }
    IMOVEL {
        uuid id PK
        uuid cliente_id FK
        varchar nome
        varchar tipo
        varchar numero_iptu
        varchar numero_matricula
        varchar status
    }
    INQUILINO {
        uuid id PK
        uuid cliente_id FK
        varchar tipo_pessoa
        varchar nome
        varchar documento
        varchar status
    }
    CONTRATO {
        uuid id PK
        uuid imovel_id FK
        uuid inquilino_id FK
        varchar numero_contrato
        varchar status
        date data_inicio
        int dia_vencimento
        numeric valor_aluguel
        numeric juros_atraso_pct
        numeric multa_atraso_pct
        text anexo_path
    }
    NOTA_FISCAL_SERVICO {
        uuid id PK
        uuid imovel_id FK
        uuid contrato_id FK
        char competencia
        bigint numero
        varchar serie
        varchar chave_acesso
        varchar status
        numeric valor_faturado
        uuid usuario_emissor_id
        text motivo_cancelamento
        varchar protocolo_cancelamento
    }
    DOCUMENTO_FISCAL {
        uuid id PK
        uuid nfse_id FK
        varchar tipo
        text storage_path
        varchar content_hash
    }
    PAGAMENTO {
        uuid id PK
        uuid imovel_id FK
        uuid nfse_id FK
        char competencia
        numeric valor_pago
        date data_pagamento
    }
    DESPESA {
        uuid id PK
        uuid imovel_id FK
        varchar tipo
        varchar categoria
        varchar fornecedor
        char competencia
        numeric valor
    }
```

## 3. Infraestrutura (sem FK ao domínio) e Identity

```mermaid
erDiagram
    ASP_NET_USERS ||--o{ ASP_NET_USER_ROLES : tem
    ASP_NET_ROLES ||--o{ ASP_NET_USER_ROLES : atribui

    AUDIT_LOG {
        bigint id PK
        uuid tenant_id
        uuid usuario_id
        varchar acao
        varchar tabela
        uuid registro_id
        jsonb valores_antes
        jsonb valores_depois
        inet ip
    }
    OUTBOX_MESSAGE { uuid id PK  varchar tipo  jsonb conteudo  timestamptz processado_em }
    INBOX_MESSAGE  { uuid id PK  varchar origem  varchar chave_externa  timestamptz processado_em }
    ASP_NET_USERS  { uuid id PK  citext email  text password_hash  uuid tenant_id  varchar cpf }
    ASP_NET_ROLES  { uuid id PK  varchar name }
    ASP_NET_USER_ROLES { uuid user_id PK  uuid role_id PK }
```

> `USUARIO` (domínio) = `ASP_NET_USERS` estendido (`AppUser : IdentityUser<Guid>`), mantido no schema `identity`. A atuação em cada cliente é `app.usuario_cliente` (perfil e status por vinculação).

## 4. Cardinalidades e regras

| Relacionamento | Cardinalidade | Regra |
|---|---|---|
| `cliente` → `imovel` | 1 : N | Limitado por `plano.max_imoveis` (CASO 2). |
| `cliente` → `usuario_cliente` → `usuario` | N : N | CPF identifica a pessoa (único no tenant). Um CPF não se repete no mesmo cliente (CASO 1); o mesmo CPF pode atuar em vários clientes, cada um com seu perfil. `plano.max_usuarios` conta vinculações ativas. |
| `cliente` → `certificado_digital` | 1 : 0..1 ativo | Obrigatório para emitir NFS-e. |
| `imovel` → `contrato` | 1 : N (1 ativo) | Só 1 contrato `Ativo` por imóvel ⇒ 1 inquilino por vez (CASO 3). |
| `contrato` → `nota_fiscal_servico` | 1 : N | 1 faturamento não-cancelado por competência (CASO 6/7/8). |
| `nota_fiscal_servico` → `pagamento` | 1 : 0..1 | `pagamento.nfse_id` opcional (§12). |
| `nota_fiscal_servico` → `documento_fiscal` | 1 : N | XML, PDF e XML do evento de cancelamento. |
| `imovel` → `pagamento` / `despesa` | 1 : N | Pagamento único por competência (CASO 6). |
| `assinatura` → `pagamento_plano` | 1 : N | Cobranças Mercado Pago. |

Próximo: [05 — Entidades do Entity Framework](05-Entidades-EntityFramework.md).
