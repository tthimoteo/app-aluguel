# Plano de Implementação de Nova Plataforma para Controle de Recebimento de Aluguéis com Emissão de NFS-e

## Objetivo

Desenvolver uma plataforma SaaS para gestão de locações, recebimentos, inadimplência e emissão de NFS-e conforme regras da reforma tributária do Brasil.

As regras detalhadas deverão seguir a especificação revisada aprovada para o projeto, incluindo:

- Emissão de NFS-e
- Cancelamento
- Controle de planos
- Armazenamento de documentos fiscais
- Trilha de auditoria

---

## Arquitetura

Aplicação web responsiva, multi-tenant, hospedada em subdomínio da Lucrare.

URL:

https://lucrarecontabilidade.com.br

---

# Planos

## Trial

- 7 dias
- 1 cliente
- 1 usuário
- Sem emissão de NFS-e

## Básico

- Até 3 imóveis
- 1 usuário
- Sem emissão de NFS-e
- Valor: R$ 49,99

Pagamento:

- PIX
- Cartão de crédito

Recebedor:

**Lucrare Contabilidade Estratégia LTDA**
CNPJ: 51.095.456/0001-13

## Intermediário

- Até 5 imóveis
- Até 3 usuários
- Com emissão de NFS-e
- Valor: R$ 149,99

## Avançado

- Até 10 imóveis
- Até 5 usuários
- Com emissão de NFS-e
- Valor: R$ 249,99

## Pro

- Mais de 10 imóveis
- Valor sob consulta

---

# Fluxo de Cobrança SaaS

## Controle de Assinaturas

Integração com Mercado Pago para:

- Contratação
- Renovação
- Suspensão
- Reativação

---

## Período Trial

Ao criar conta:

- Trial automático de 7 dias

Funcionalidades liberadas:

- Cadastro de imóveis
- Cadastro de inquilinos
- Cadastro de usuários
- Dashboard

Restrições:

- Emissão de NFS-e bloqueada

Após o término:

- Suspensão automática
- Ou liberação manual pelo administrador

---

## Contratação de Plano

Fluxo:

1. Cliente acessa "Minha Conta"
2. Seleciona plano
3. Sistema cria assinatura
4. Status: `Pendente de Pagamento`
5. Redireciona ao Mercado Pago
6. Após pagamento:
   - Ativação automática
   - Liberação fiscal quando aplicável

---

## Renovação da Assinatura

O sistema deverá:

- Gerar nova cobrança
- Aguardar confirmação do Mercado Pago
- Registrar movimentação financeira

---

## Falha de Pagamento

Status:

`Pendente de Pagamento`

Ações:

- Enviar e-mail
- Exibir alerta
- Registrar auditoria

---

## Período de Tolerância

- 7 dias corridos

Durante o período:

- Todas as funções permanecem liberadas

---

## Suspensão por Inadimplência

Após o período de tolerância:

Status:

`Suspensa`

Permitido:

- Login
- Minha Conta
- Pagamentos

Bloqueado:

- Todo o restante da plataforma

---

## Reativação

Quando o Mercado Pago informar o pagamento:

- Reativa automaticamente
- Libera funcionalidades
- Registra auditoria
- Atualiza situação financeira

---

## Upgrade

Permitido a qualquer momento.

Exemplos:

- Básico → Intermediário
- Básico → Avançado
- Intermediário → Avançado
- Avançado → Pro

---

## Downgrade

Valida:

- Quantidade de imóveis
- Quantidade de usuários
- Limites do plano

Caso exceda os limites:

- Operação bloqueada

---

## Cancelamento

Ao cancelar:

- Não gerar novas cobranças
- Permanecer ativo até o fim do ciclo pago
- Depois mudar para `Suspensa`

Os dados não devem ser excluídos.

---

# Status da Assinatura

- Trial
- Pendente de Pagamento
- Ativa
- Suspensa
- Cancelada

---

# Auditoria de Assinaturas

Registrar:

- Início Trial
- Contratação
- Renovação
- Pagamento
- Falha de pagamento
- Suspensão
- Reativação
- Upgrade
- Downgrade
- Cancelamento

Campos:

- Cliente
- Usuário
- Data/Hora
- IP
- Plano anterior
- Novo plano
- Valor
- Descrição

---

# Entidades

## Assinatura

```text
Assinatura
├── Id
├── ClienteId
├── Plano
├── Status
├── DataInicio
├── DataFimTrial
├── ProximaCobranca
├── ValorMensal
├── MercadoPagoSubscriptionId
├── CreatedAt
└── UpdatedAt
```

## PagamentoPlano

```text
PagamentoPlano
├── Id
├── AssinaturaId
├── Valor
├── DataVencimento
├── DataPagamento
├── Status
├── MetodoPagamento
├── MercadoPagoPaymentId
└── Observacao
```

---

# Fluxo Resumido

```text
Trial 7 dias
    ↓
Mercado Pago Assinaturas
    ↓
Webhook
    ↓
Ativa
    ↓
Suspensa por inadimplência
    ↓
Reativação automática
```

---

# Usuário Administrador

Permissões:

- Gerenciar clientes
- Gerenciar usuários
- Gerenciar imóveis
- Gerenciar inquilinos
- Emitir NFS-e
- Cancelar NFS-e

---

# Cadastro de Clientes

Tipos:

- Pessoa Física
- Pessoa Jurídica

## Pessoa Física

Campos:

- Id
- TenantId
- Tipo
- Plano
- Status
- Nome
- CPF
- Data de nascimento
- Telefone
- E-mail
- Endereço completo

## Pessoa Jurídica

Campos:

- Id
- TenantId
- Tipo
- Plano
- Status
- Razão Social
- Nome Fantasia
- CNPJ
- Inscrição Municipal
- CNAE
- Telefone
- E-mail
- Endereço completo

---

# Certificado Digital

Obrigatório para emissão de NFS-e.

Permitir:

- Upload PFX/P12
- Senha

Requisitos:

- Armazenamento criptografado
- Uso apenas pelo backend
- Nunca expor senha ao frontend

## Entidade

```text
CertificadoDigital
├── Id
├── TenantId
├── Arquivo
├── Thumbprint
├── Validade
├── SenhaCriptografada
└── CreatedAt
```

---

# Usuários

Campos:

- Id
- ClienteId
- Perfil
- Nome
- Senha
- CPF
- E-mail
-