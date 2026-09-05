# 04 — ERD (Entity Relationship Diagram)

Diagrama entidade-relacionamento completo da plataforma APP Aluguel. Renderiza no GitHub/Cursor (Mermaid).

## 1. Diagrama completo

```mermaid
erDiagram
    TENANT ||--o{ CLIENTE : possui
    PLANO  ||--o{ CLIENTE : "plano atual"
    PLANO  ||--o{ ASSINATURA : referencia

    CLIENTE ||--o{ USUARIO : "tem"
    CLIENTE ||--o{ ASSINATURA : "assina"
    CLIENTE ||--o{ IMOVEL : "cadastra"
    CLIENTE ||--o{ INQUILINO : "cadastra"
    CLIENTE ||--o{ CONTRATO : "gerencia"
    CLIENTE ||--o{ CERTIFICADO_DIGITAL : "possui"
    CLIENTE ||--o{ NOTA_FISCAL_SERVICO : "emite"
    CLIENTE ||--o{ AUDITORIA_ASSINATURA : "registra"

    ASSINATURA ||--o{ PAGAMENTO_PLANO : "gera"
    ASSINATURA ||--o{ AUDITORIA_ASSINATURA : "audita"

    IMOVEL ||--o{ CONTRATO : "objeto de"
    INQUILINO ||--o{ CONTRATO : "parte de"
    CONTRATO ||--o{ RECEBIMENTO : "origina"

    RECEBIMENTO ||--o| NOTA_FISCAL_SERVICO : "documenta"
    NOTA_FISCAL_SERVICO ||--o{ DOCUMENTO_FISCAL : "arquivos"

    USUARIO ||--o{ AUDITORIA_ASSINATURA : "executa"

    TENANT {
        uuid id PK
        varchar nome
        varchar subdominio UK
        boolean ativo
    }

    PLANO {
        uuid id PK
        varchar codigo UK
        varchar nome
        int max_imoveis
        int max_usuarios
        boolean permite_nfse
        int trial_dias
        numeric valor_mensal
    }

    CLIENTE {
        uuid id PK
        uuid tenant_id FK
        varchar tipo_pessoa
        uuid plano_id FK
        varchar status
        varchar nome
        varchar cpf
        varchar razao_social
        varchar cnpj
        varchar inscricao_municipal
        varchar cnae
        citext email
        timestamptz deleted_at
    }

    USUARIO {
        uuid id PK
        uuid tenant_id FK
        uuid cliente_id FK
        varchar perfil
        varchar nome
        citext email
        uuid auth_user_id
        boolean ativo
    }

    ASSINATURA {
        uuid id PK
        uuid tenant_id FK
        uuid cliente_id FK
        uuid plano_id FK
        varchar status
        timestamptz data_inicio
        timestamptz data_fim_trial
        timestamptz proxima_cobranca
        timestamptz inicio_tolerancia
        timestamptz fim_ciclo_pago
        numeric valor_mensal
        varchar mercadopago_subscription_id
    }

    PAGAMENTO_PLANO {
        uuid id PK
        uuid tenant_id FK
        uuid assinatura_id FK
        numeric valor
        timestamptz data_vencimento
        timestamptz data_pagamento
        varchar status
        varchar metodo_pagamento
        varchar mercadopago_payment_id UK
    }

    AUDITORIA_ASSINATURA {
        uuid id PK
        uuid tenant_id FK
        uuid assinatura_id FK
        uuid cliente_id FK
        uuid usuario_id FK
        varchar evento
        timestamptz data_hora
        inet ip
        varchar plano_anterior
        varchar novo_plano
        numeric valor
        text descricao
    }

    IMOVEL {
        uuid id PK
        uuid tenant_id FK
        uuid cliente_id FK
        varchar codigo
        varchar tipo
        numeric valor_aluguel
        varchar status
        timestamptz deleted_at
    }

    INQUILINO {
        uuid id PK
        uuid tenant_id FK
        uuid cliente_id FK
        varchar tipo_pessoa
        varchar nome
        varchar documento
        citext email
        timestamptz deleted_at
    }

    CONTRATO {
        uuid id PK
        uuid tenant_id FK
        uuid cliente_id FK
        uuid imovel_id FK
        uuid inquilino_id FK
        numeric valor_aluguel
        int dia_vencimento
        date data_inicio
        date data_fim
        varchar status
    }

    RECEBIMENTO {
        uuid id PK
        uuid tenant_id FK
        uuid cliente_id FK
        uuid contrato_id FK
        char competencia
        numeric valor
        date data_vencimento
        date data_pagamento
        varchar status
    }

    CERTIFICADO_DIGITAL {
        uuid id PK
        uuid tenant_id FK
        uuid cliente_id FK
        text storage_path
        varchar thumbprint
        timestamptz validade
        bytea senha_cifrada
        boolean ativo
    }

    NOTA_FISCAL_SERVICO {
        uuid id PK
        uuid tenant_id FK
        uuid cliente_id FK
        uuid recebimento_id FK
        bigint numero_rps
        bigint numero_nfse
        varchar codigo_verificacao
        numeric valor_servico
        numeric valor_iss
        varchar status
        timestamptz emitida_em
        timestamptz cancelada_em
    }

    DOCUMENTO_FISCAL {
        uuid id PK
        uuid tenant_id FK
        uuid nfse_id FK
        varchar tipo
        text storage_path
        varchar content_hash
        bigint tamanho_bytes
    }
```

## 2. Tabelas de infraestrutura (não relacionadas por FK ao domínio)

```mermaid
erDiagram
    AUDIT_LOG {
        bigint id PK
        uuid tenant_id
        varchar entidade
        uuid entidade_id
        varchar acao
        uuid usuario_id
        jsonb dados_antes
        jsonb dados_depois
        timestamptz data_hora
    }
    OUTBOX_MESSAGE {
        uuid id PK
        uuid tenant_id
        varchar tipo
        jsonb conteudo
        timestamptz ocorrido_em
        timestamptz processado_em
        int tentativas
    }
    INBOX_MESSAGE {
        uuid id PK
        varchar origem
        varchar chave_externa
        jsonb payload
        timestamptz processado_em
    }
```

## 3. Cardinalidades e regras

| Relacionamento | Cardinalidade | Regra de negócio |
|---|---|---|
| `tenant` → `cliente` | 1 : N | Isolamento multi-tenant; na prática 1 conta = 1 cliente principal. |
| `plano` → `cliente` | 1 : N | Plano vigente do cliente (limites de imóveis/usuários). |
| `cliente` → `usuario` | 1 : N | Limitado por `plano.max_usuarios`. |
| `cliente` → `assinatura` | 1 : N (1 vigente) | Índice único garante 1 assinatura em Trial/Pendente/Ativa. |
| `assinatura` → `pagamento_plano` | 1 : N | Histórico de cobranças Mercado Pago. |
| `assinatura`/`cliente` → `auditoria_assinatura` | 1 : N | Um registro por transição de estado. |
| `cliente` → `imovel` | 1 : N | Limitado por `plano.max_imoveis`. |
| `cliente` → `inquilino` | 1 : N | — |
| `imovel` + `inquilino` → `contrato` | N : 1 cada | Um contrato liga um imóvel a um inquilino. |
| `contrato` → `recebimento` | 1 : N | Uma parcela por competência (índice único). |
| `recebimento` → `nota_fiscal_servico` | 1 : 0..1 | NFS-e opcional, só em planos com `permite_nfse`. |
| `nota_fiscal_servico` → `documento_fiscal` | 1 : N | XML, PDF e RPS armazenados no Supabase Storage. |
| `cliente` → `certificado_digital` | 1 : N (1 ativo) | Certificado A1 obrigatório para emitir NFS-e. |

## 4. Notas de integridade

- Todas as FKs de negócio carregam também `tenant_id` para reforço de isolamento e índices compostos eficientes.
- `deleted_at` implementa *soft delete* — no cancelamento de assinatura os dados são preservados (exigência da especificação).
- Chaves de idempotência: `pagamento_plano.mercadopago_payment_id` (único) e `inbox_message(origem, chave_externa)` (único).

Próximo: [05 — Entidades do Entity Framework](05-Entidades-EntityFramework.md).
