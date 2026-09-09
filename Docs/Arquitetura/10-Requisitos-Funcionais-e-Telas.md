# 10 — Requisitos Funcionais e Telas

Detalha as telas e regras funcionais (§10–§14) e como mapeiam para endpoints/entidades.

## 1. Tela inicial — Home (§10)

**Filtros:** Competência (MM/AAAA), Imóvel, Inquilino, NFS-e pendente, Pagamento pendente.

**Indicadores (cards + Recharts):** faturamento, inadimplência, quantidade de imóveis, quantidade de notas emitidas.

**Botão "Incluir imóvel"** — visível conforme quantidade já cadastrada **e** plano contratado (`plano.max_imoveis`).

**Administrador:** a lista da home é de **clientes** (nome/razão social, CPF ou CNPJ, plano, status), não de imóveis. Os cards de indicadores exibem quantidade de **clientes**, imóveis e planos (sem inquilinos nem contratos). Gestor e Analista continuam com a lista de imóveis e os cards de imóveis, inquilinos, contratos e planos.

**Lista de imóveis** (Gestor/Analista), por linha:
- Situação de NFS-e e pagamento: `Dentro do prazo` · `NFS-e pendente` · `Inadimplente`;
- Próxima data de vencimento do aluguel **ou** última data de vencimento sem emissão de NFS-e;
- Nome do inquilino;
- Botão **Emitir NFS-e** (só se plano permite e assinatura ativa);
- Botão **Registrar pagamento**;
- Botão **"..."** (menu por perfil): editar imóvel, editar inquilino, consultar histórico, registrar pagamento IPTU, registrar outras despesas.

**Endpoint:** `GET /home?competencia=&imovelId=&inquilinoId=&nfsePendente=&pagamentoPendente=` → indicadores + lista paginada.

### Regra de situação (derivação)

```text
se existe NFS-e (status Emitida) na competência e pagamento registrado  -> Dentro do prazo
se não existe NFS-e na competência (e há aluguel devido)                -> NFS-e pendente
se vencimento passou sem pagamento                                      -> Inadimplente
```

## 2. Emitir NFS-e (§11)

Tela de emissão permite: **valor de descontos**, edição de **multas** e **juros**. Ao **Emitir**, integra a **API Nacional da NFS-e**.

**Pré-condições:** plano permite NFS-e (**CASO 4**); assinatura `Ativa` (**CASO 5**); não há faturamento na competência com status ≠ `Cancelada` (**CASO 8**); certificado A1 válido.

**Fluxo (§11):** solicita → valida competência → gera faturamento → integra API Nacional → recebe retorno → armazena XML → armazena PDF → registra chave → registra número → atualiza histórico (usuário emissor + data/hora).

**Endpoint:** `POST /nfse` `{ imovelId, competencia, desconto, multa, juros }` → cria faturamento e enfileira emissão.

**Persistência:** `nota_fiscal_servico` + `documento_fiscal` (XML/PDF) — doc 03.

## 3. Registrar pagamento / IPTU / despesas (§12)

Pop-up simples: **competência** e **data de pagamento** (aluguel). IPTU e "outras despesas" seguem o mesmo modelo + **descrição**.

**Regra:** um pagamento já feito **não** pode ser repetido para a mesma competência (**CASO 6**).

**Endpoints:**
- `POST /pagamentos` `{ imovelId, competencia, dataPagamento, valorPago, nfseId? }`
- `POST /despesas` `{ imovelId, tipo: IPTU|Outra, descricao?, competencia, valor, fornecedor?, categoria? }`

## 4. Histórico e cancelamento de NFS-e (§12)

Tela mostra, **mês a mês**, o histórico de faturamento (XML e PDF para baixar) com histórico de pagamento. Ao selecionar um faturamento: **cancelar NFS-e** e **editar pagamento**.

**Fluxo de cancelamento:** solicitar → evento à API da NFS-e → registrar protocolo → registrar XML do evento → atualizar status. Armazena: motivo, protocolo, XML do evento. Todas as modificações ficam em **log do próprio registro**.

**Endpoints:**
- `GET /imoveis/{id}/historico?competencia=`
- `POST /nfse/{id}/cancelamento` `{ motivo }`
- `GET /nfse/{id}/xml` · `GET /nfse/{id}/pdf` (signed URL)

## 5. Menu (§13)

| Item | Visibilidade | Função |
|---|---|---|
| **Usuários** | Administrador (escolhe o cliente) / Gestor (próprio cliente) | Incluir, editar, remover e consultar detalhes (clique na linha). CPF identifica a pessoa: o mesmo usuário pode ser vinculado a outro cliente com perfil próprio; não pode repetir o CPF no mesmo cliente. |
| **Minha Conta** | Gestor | Dados de cliente/usuário; histórico de cobrança do app; plano atual; **upgrade**. |
| **Relatório** | Administrador/Gestor/Analista | Extrair histórico de faturamentos, pagamentos, IPTU e demais despesas. |
| **Dados para Contabilidade** | Gestor | Exportar Contas a Receber e Contas a Pagar em XLSX/CSV. |
| **Auditoria** | AdminSistema/Gestor | Consultar logs de alterações. |
| **Tema** | Todos | Claro/escuro. |
| **Sair** | Todos | Logout (revoga refresh token). |

Quem tiver vinculação em mais de um cliente escolhe o contexto no menu do usuário (`POST /api/auth/contexto`); o JWT passa a carregar o `cliente_id` e o perfil daquela vinculação.

### Dados para Contabilidade (§13)

| Exportação | Base | Campos | Formatos |
|---|---|---|---|
| **Contas a Receber** | NFS-e emitidas | competência, valor, cliente, número NFS-e | XLSX, CSV |
| **Contas a Pagar** | IPTU e despesas | competência, fornecedor, valor, categoria | XLSX, CSV |

Geração via **ClosedXML** (XLSX/CSV) e **QuestPDF** (PDF); arquivos no bucket `reports`.

## 6. Auditoria (§13)

Toda alteração gera log (`audit_log`): usuário, data, hora, ação, IP, valores antes, valores depois. Eventos: **login, emissão, cancelamento, pagamento, despesas, alteração cadastral**.

## 7. Requisitos não funcionais (§14)

| Categoria | Requisito | Como atendido |
|---|---|---|
| Segurança | HTTPS obrigatório | TLS em todos os ambientes |
| Segurança | Criptografia de senhas | ASP.NET Identity (PBKDF2) |
| Segurança | MFA opcional | TOTP (Identity) |
| Segurança | Segregação total entre tenants | `tenant_id` + RLS (doc 06) |
| Segurança | LGPD | soft delete, auditoria, mínimo de dados |
| Performance | Tela < 2s | índices, cache Redis, projeções |
| Performance | Paginação em listas | *cursor/offset* + limites |
| Backup | Diário, retenção ≥ 90 dias | PITR Supabase + export fiscal |

## 8. Regras de negócio consolidadas

- Remoção de cadastro só se **não houver dependentes** (§4).
- Cliente PF: primeiro usuário = cliente, perfil **Gestor** (§6).
- CPF do usuário é a chave da pessoa: único no tenant; único por cliente na vinculação (CASO 1); o mesmo CPF pode atuar em vários clientes com perfil por vinculação (CASO 1b).
- Imóvel tem **1 inquilino por vez** (1 contrato ativo) — §8/§9, CASO 3.
- Emissão de NFS-e só com plano compatível e assinatura ativa — CASO 4/5.
- 1 faturamento não-cancelado por competência — CASO 6/7/8.

Próximo: [11 — Casos de Uso e Critérios de Aceite](11-Casos-de-Uso-e-Criterios-de-Aceite.md).
