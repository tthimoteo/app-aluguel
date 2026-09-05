# Plano de Implementação de Nova Plataforma para Controle de Recebimento de Aluguéis com Emissão de NFS-e

> Transcrição completa e organizada da especificação aprovada (fonte: `APP_Aluguel.pdf`, 20 páginas).

## 1. Objetivo

Desenvolver uma plataforma **SaaS** para gestão de locações, recebimentos, inadimplência e emissão de **NFS-e** conforme regras da reforma tributária do Brasil.

As regras detalhadas deverão seguir a especificação revisada aprovada para o projeto, incluindo fluxos de **emissão**, **cancelamento**, **controle de planos**, **armazenamento de documentos fiscais** e **trilha de auditoria**.

## 2. Arquitetura

Aplicação web **responsiva**, **multi-tenant**, hospedada em **subdomínio da Lucrare**. O app deve ser acessado por URL hospedada em subdomínio de `https://lucrarecontabilidade.com.br`.

## 3. Planos

| Plano | Imóveis | Usuários | NFS-e | Preço | Pagamento |
|---|---|---|---|---|---|
| **Trial** | 1 cliente / imóvel | 1 | Não | Grátis (7 dias) | — |
| **Básico** | até 3 | 1 | Não | R$ 49,99 | PIX ou cartão |
| **Intermediário** | até 5 | até 3 | Sim | R$ 149,99 | PIX ou cartão |
| **Avançado** | até 10 | até 5 | Sim | R$ 249,99 | PIX ou cartão |
| **Pro** | mais de 10 | — | Sim | Negociado via contato | — |

A cobrança será destinada para a CC da **Lucrare Contabilidade Estratégia LTDA – CNPJ 51.095.456/0001-13**.

### Fluxo de cobrança da plataforma SaaS

O sistema deverá possuir controle automatizado de assinaturas dos planos contratados pelos clientes, permitindo **contratação, renovação, suspensão e reativação** de forma automática através da integração com o **Mercado Pago**.

#### Período de Trial

Ao realizar o cadastro inicial, o cliente receberá automaticamente um período de avaliação (Trial) de **7 dias**. Durante o Trial são permitidas:
- Cadastro de imóveis;
- Cadastro de inquilinos;
- Cadastro de usuários;
- Consulta do dashboard inicial.

Durante o Trial **não** será permitida a emissão de NFS-e. Ao término, caso nenhuma assinatura tenha sido contratada, o acesso será **suspenso automaticamente** ou poderá ser liberado manualmente pelo usuário administrador do sistema.

#### Contratação de Plano

O cliente contrata um plano em **"Minha Conta"**. Ao selecionar um plano, o sistema deverá:
- Registrar a solicitação de contratação;
- Criar registro de assinatura com status **"Pendente de Pagamento"**;
- Redirecionar o usuário ao ambiente de pagamento do Mercado Pago.

Após a confirmação do pagamento, a assinatura é **ativada automaticamente**. Se o plano permitir NFS-e, as funcionalidades fiscais são liberadas imediatamente após a confirmação.

#### Renovação da Assinatura

Renovação automática conforme periodicidade contratada. A cada renovação o sistema deverá: gerar novo registro de cobrança; aguardar confirmação do Mercado Pago; registrar a movimentação financeira. Com pagamento identificado, a assinatura permanece ativa sem intervenção manual.

#### Falha no Pagamento

Quando o pagamento de uma renovação não for identificado até o vencimento, a assinatura passa a **"Pendente de Pagamento"**. O sistema deverá: notificar o cliente por e-mail; exibir alerta visual; registrar a ocorrência na auditoria.

#### Período de Tolerância

Após o vencimento, conceder **7 dias corridos** de tolerância. Durante esse período o cliente continua com acesso normal às funcionalidades do plano.

#### Suspensão por Inadimplência

Se o pagamento não for regularizado na tolerância, a assinatura passa a **"Suspensa"**. Durante a suspensão:
- Login permitido;
- Tela "Minha Conta" permitida;
- Informações de pagamento permitidas;
- **Todas as demais funcionalidades bloqueadas**.

O sistema deve exibir mensagem orientando a regularização.

#### Reativação da Assinatura

Sempre que o Mercado Pago informar a compensação de um pagamento pendente, o sistema reativa **automaticamente** a assinatura: restaura funcionalidades; registra na auditoria; atualiza a situação financeira.

#### Upgrade de Plano

Permitido a qualquer momento. Ao solicitar: alterar imediatamente os limites; registrar a alteração; registrar histórico. Exemplos: Básico→Intermediário, Básico→Avançado, Intermediário→Avançado, Avançado→Pro.

#### Downgrade de Plano

Permitido desde que a utilização atual seja compatível com os limites do plano desejado. Antes de efetivar, validar: quantidade de imóveis; quantidade de usuários; demais limites. Se excedidos, o downgrade é **bloqueado** até adequação dos dados.

#### Cancelamento da Assinatura

Solicitado em "Minha Conta". Ao cancelar: nenhuma nova cobrança é gerada; o plano permanece ativo até o fim do ciclo já pago; após o ciclo, a assinatura passa a **"Suspensa"**. **Nenhum dado do cliente é excluído** automaticamente.

#### Status da Assinatura

`Trial` · `Pendente de Pagamento` · `Ativa` · `Suspensa` · `Cancelada`

#### Auditoria de Assinaturas

Registrar (no mínimo): Início do Trial, Contratação, Renovação, Pagamento recebido, Falha de pagamento, Suspensão por inadimplência, Reativação, Upgrade, Downgrade, Cancelamento.

Cada registro armazena: Cliente; Usuário responsável; Data e hora; IP; Plano anterior; Novo plano; Valor da operação; Descrição da ação.

### Entidades de assinatura

```text
Assinatura                         PagamentoPlano
├── Id                             ├── Id
├── ClienteId                      ├── AssinaturaId
├── Plano                          ├── Valor
├── Status                         ├── DataVencimento
├── DataInicio                     ├── DataPagamento
├── DataFimTrial                   ├── Status
├── ProximaCobranca                ├── MetodoPagamento
├── ValorMensal                    ├── MercadoPagoPaymentId
├── MercadoPagoSubscriptionId      └── Observacao
├── CreatedAt
└── UpdatedAt
```

### Resumo do fluxo de pagamento SaaS

```text
Trial 7 dias → Mercado Pago Assinaturas → Webhook → Ativa
   → Suspensa por inadimplência → Reativação automática
```

## 4. Usuário administrador do sistema

O usuário administrador do app poderá fazer **todas** as tarefas, bem como criar, editar e remover dados de clientes, usuários, imóveis e inquilinos. **A remoção só poderá ocorrer caso não haja outros registros dependentes** do cadastro.

## 5. Cadastro de clientes

Um cliente pode ser **Pessoa Física (CPF)** ou **Jurídica (CNPJ)**. O cadastro deve conter as informações necessárias para emissão de NFS-e (**emitente da NFS-e = locador**), incluindo a necessidade de **anexar certificado digital A1 com senha**, e para cobrança dos planos. A cobrança do serviço do APP é direcionada ao cliente cadastrado.

O administrador poderá editar, inativar, remover ou criar um cliente. A remoção só ocorre se não houver registros dependentes.

**Pessoa Física**: ID, TenantId, Tipo, Plano, Status, Nome, CPF, Data de nascimento, Telefone, E-mail, Endereço completo.

**Pessoa Jurídica**: ID, TenantId, Tipo, Plano, Status, Razão Social, Nome Fantasia, CNPJ, Inscrição Municipal, CNAE principal, Telefone, E-mail, Endereço completo.

### Certificado Digital

Obrigatório para clientes que emitirem NFS-e. Permitir upload de arquivo A1 (PFX/P12) e senha do certificado. Requisitos: armazenamento **criptografado**, acesso **apenas pelo backend**, **nunca** expor a senha ao frontend.

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

## 6. Cadastro do usuário do cliente

Após o cadastro do cliente, é necessário cadastrar o usuário que fará uso do app. Se o cliente for **PF**, o cadastro do **primeiro usuário coincide** com o do cliente e deve ter perfil **"Gestor"**.

Um usuário associado a um cliente **não pode, de forma alguma, visualizar dados de outro cliente** ao qual não esteja associado, mesmo com perfil "Gestor".

O usuário "Gestor" poderá cadastrar outros usuários conforme a permissão do plano — com perfil "Gestor" ou "Analista".

**Campos**: Id, ClienteId, Perfil, Nome, Senha, CPF, E-mail, Telefone, Perfil (Gestor ou Analista), Status (Ativo; Inativo; Bloqueado).

**Perfil "Gestor"** permite:
- Editar as próprias informações como cliente, inclusive mudar de plano;
- Criar, editar e remover usuários associados ao cliente;
- Incluir, editar e remover informações de imóveis e inquilinos;
- Emitir NFS-e para os imóveis;
- Consultar histórico de emissão de NFS-e e valores de cobrança dos aluguéis.

**Perfil "Analista"** permite:
- Consultar informações de imóveis e inquilinos;
- Emitir NFS-e para os imóveis;
- Consultar histórico de emissão de NFS-e e valores de cobrança dos aluguéis;
- Cancelar NFS-e.

## 7. Cadastro de imóvel

O usuário gestor pode cadastrar, editar, inativar e remover um imóvel e respectivo inquilino conforme o plano. O cadastro do imóvel contém:
- Id (automático); ClienteId; Nome;
- Endereço completo;
- Tipo de imóvel: residencial, comercial, galpão, sala etc.;
- Nr. IPTU; Nr. Matrícula;
- Status: ativo; inativo.

## 8. Cadastro do inquilino

- Id (automático); ImovelId; InquilinoId;
- Tipo: pessoa física ou jurídica;
- Nome ou razão social; CPF ou CNPJ; Endereço completo; Telefone; E-mail;
- Status: ativo; inativo;
- **+ toda informação necessária para emissão de NFS-e**, sendo o inquilino o **destinatário da NFS-e**.

Um inquilino pode ser inativado de um imóvel e outro cadastrado no lugar. O **imóvel só pode ter um inquilino por vez**. A inativação é feita manualmente pelo gestor.

## 9. Cadastro de contrato

- ID (automático); Nr. Contrato de aluguel;
- Status: Ativo; Encerrado; Cancelado;
- Data de início; Data prevista de término; Data de vencimento do aluguel;
- Valor do aluguel;
- Juros por atraso; Multa por atraso;
- Possibilidade de anexar o contrato;
- Possibilidade de renovação, reajuste, aditivo.

## 10. Tela inicial (Home)

**Filtros**: Competência (MM/AAAA); Imóvel; Inquilino; NFS-e pendente; Pagamento pendente.

**Indicadores**: faturamento; inadimplência; quantidade de imóveis; quantidade de notas emitidas.

Botão de **inclusão de imóvel** (visível conforme quantidade cadastrada e plano). Lista de imóveis com:
- Situação de emissão de NFS-e e pagamento (dentro do prazo, NFS-e pendente, inadimplência);
- Próxima data de vencimento do aluguel ou última data de vencimento sem emissão de NFS-e;
- Nome do inquilino;
- Botão **"Emitir NFS-e"**;
- Botão **"Registrar pagamento"**;
- Botão **"..."** com menu conforme o perfil (editar imóvel; editar inquilino; consultar histórico; registrar pagamento de IPTU; registrar outras despesas).

## 11. Emitir NFS-e

Somente conforme disponibilidade do plano. Ao clicar, abre tela que permite: inserir valor de descontos; editar multas e juros.

Ao clicar em **"Emitir"**, o app integra via **API Nacional da NFS-e** para disponibilização do **XML, PDF, status e chave de acesso**, os quais ficam disponíveis para download futuramente. O Nr. da NFS-e é cadastrado no histórico de faturamento, com o valor faturado. Os dados de **quem solicitou** e **quando** são gravados no histórico de faturamento do imóvel.

Um faturamento só pode ocorrer se, naquele mês de competência, **não houver outro faturamento com status diferente de cancelado** (ou se não houver faturamento anterior).

**Fluxo**: 1) usuário solicita; 2) valida competência; 3) gera faturamento; 4) integra API Nacional; 5) recebe retorno; 6) armazena XML; 7) armazena PDF; 8) registra chave de acesso; 9) registra número; 10) atualiza histórico.

**Dados armazenados**: Id; Competência; Número; Série; ChaveAcesso; Status (Rascunho, Em Processamento, Emitida, Rejeitada, Cancelamento Solicitado, Cancelada); XML; PDF; Data emissão; Usuário emissor.

## 12. Registrar pagamento

Pop-up simples: competência do aluguel pago e data de pagamento. **Um pagamento já feito não pode ser repetido.** As telas "Registrar IPTU" e "Registrar outras despesas" seguem o mesmo modelo, com campo de descrição.

**Armazenar**: Id; NfseId (opcional, caso NFS-e não emitida); DataPagamento; ValorPago.

### Consultar histórico e cancelar NFS-e

A tela de histórico permite ver, mês a mês, o histórico de faturamento (XML e PDF para baixar) com o histórico de pagamento. Ao selecionar um faturamento, o usuário pode **cancelar a NFS-e** e editar informações do pagamento.

**Fluxo de cancelamento**: 1) solicitar cancelamento; 2) enviar evento à API da NFS-e; 3) registrar protocolo; 4) registrar XML do evento; 5) atualizar status.

**Armazenar**: motivo; protocolo; XML do evento de cancelamento.

Todas as modificações são gravadas em log do próprio registro de histórico e disponibilizadas para consulta na mesma tela.

## 13. Menu do usuário (em todas as telas)

- **Clientes** (somente administrador do sistema): gestão de clientes e respectivos planos, usuários, imóveis e inquilinos; ações de emissão/cancelamento de NFS-e e registro de pagamento (reutilizando as telas originais por submenu).
- **Minha conta** (somente gestor): consultar dados de cliente e usuário; histórico de cobrança do app; plano contratado; possibilidade de upgrade.
- **Relatório**: extrair lista de histórico de faturamentos, pagamentos, despesas de IPTU e demais despesas.
- **Dados para Contabilidade** — exportar:
  - **Contas a Receber** (base: NFS-e emitidas) — campos: competência, valor, cliente, número NFS-e.
  - **Contas a Pagar** (base: IPTU e despesas) — campos: competência, fornecedor, valor, categoria.
  - Formatos: **XLSX**, **CSV**.
- **Auditoria**: toda alteração gera log. Registrar: usuário, data, hora, ação, IP, valores antes, valores depois. Eventos auditáveis: login, emissão, cancelamento, pagamento, despesas, alteração cadastral.
- **Tema**: claro ou escuro.
- **Sair**.

```text
AuditLog
├── Id
├── TenantId
├── UsuarioId
├── Acao
├── Tabela
├── RegistroId
├── ValoresAntes
├── ValoresDepois
├── DataHora
└── IP
```

## 14. Requisitos não funcionais

- **Segurança**: HTTPS obrigatório; criptografia de senhas; MFA opcional; **segregação total entre tenants**; LGPD.
- **Performance**: tempo médio de tela inferior a 2 segundos; paginação em listas.
- **Backup**: backup diário; retenção mínima de 90 dias.

## 15. Modelo ERD

```text
Cliente
├─ Imóvel (N)
├─ Usuário (N)
└─ Certificado (1)

Imóvel      └─ Contrato (N)
Inquilino   └─ Contrato (N)
Contrato    ├─ NFS-e (N)
NFS-e (1:1) Pagamentos
AuditLog
```

## 16. Storage (buckets)

`contracts` · `certificates` · `nfse-xml` · `nfse-pdf` · `reports`

## 17. Casos de uso

| Código | Caso de uso |
|---|---|
| UC001 | Cadastrar Cliente |
| UC002 | Criar Usuário |
| UC003 | Criar Imóvel |
| UC004 | Criar Contrato |
| UC005 | Emitir NFS-e |
| UC006 | Cancelar NFS-e |
| UC007 | Registrar Pagamento |
| UC008 | Gerar Relatório |

## 18. Critérios de aceite (BDD)

- **CASO 1** — Dado que já existe usuário com CPF em um cliente, quando tentar cadastrar usuário com o mesmo CPF, o sistema deve apresentar erro informando que o CPF já está cadastrado nesse cliente.
- **CASO 2** — Dado plano que permite 10 imóveis com 10 imóveis cadastrados, quando optar por downgrade, o sistema deve alertar que é preciso inativar imóveis para se enquadrar no novo plano.
- **CASO 3** — Dado contrato ativo para um inquilino em um imóvel, quando tentar cadastrar novo inquilino para o mesmo imóvel, o sistema deve impedir e alertar que é preciso inativar o contrato atual antes.
- **CASO 4** — Dado cliente com plano Básico, quando tentar emitir NFS-e, o sistema deve impedir e informar que o plano não suporta emissão fiscal.
- **CASO 5** — Dado assinatura Suspensa, quando o usuário acessar a Home, o sistema deve impedir o acesso e redirecionar para a tela de pagamento.
- **CASO 6** — Dado pagamento já registrado para uma competência, quando tentar registrar novo pagamento, o sistema deve impedir a operação.
- **CASO 7** — Dado que a NFS-e foi cancelada, quando emitir nova NFS-e para a mesma competência, o sistema deve permitir a emissão.
- **CASO 8** — Dado NFS-e emitida, em processamento ou rascunho para a competência 08/2026, quando tentar emitir nova NFS-e para a mesma competência, o sistema deve impedir e apresentar erro.

## 19. Stack tecnológico

**Frontend**: Next.js 15, React 19, TypeScript, Tailwind v4, shadcn/ui, Lucide, Recharts, next-pwa.

**Backend**: .NET 9 Web API, EF Core, FluentValidation, JWT, ASP.NET Identity, QuestPDF, ClosedXML.

**Banco**: PostgreSQL (Supabase).

**Storage**: Supabase Storage.

**Pagamentos**: Mercado Pago.

**NFS-e**: API Nacional da NFS-e (primeiro momento).

**Hospedagem**: Vercel (frontend), Render ou Azure App Service (backend), Supabase (DB + Storage).

## 20. MVP (primeira entrega)

Prioridade:
1. Home
2. Menu
3. Cadastro de clientes
4. Cadastro de usuários
5. Cadastro de imóveis
6. Cadastro de inquilinos
7. Controle de planos
8. Emissão de NFS-e
9. Histórico
10. Registro de pagamentos
11. Relatórios básicos
