# 14 — Relatórios (painel da carteira)

Página `/relatorios` (UC008) com dashboard e relatórios operacionais/financeiros/fiscais.

## Acesso

- Perfis: Administrador, Gestor, Analista.
- Escopo: Gestor/Analista restritos ao cliente do contexto; Admin pode filtrar por proprietário (cliente).

## API

`GET /api/relatorios/painel` — query params: `ano`, `mes`, `dataInicio`, `dataFim`, `clienteId`, `imovelId`, `diasAtrasoMinimos`.

Retorna um `RelatorioPainelDto` com seções: dashboard, rendimento, cobranças geradas×recebidas, inadimplentes, ranking de atrasos, fluxo de caixa previsto, rentabilidade por imóvel, vacância, contratos vencendo, fiscal NFS-e, proprietários, DRE e scores de inquilino.

## Abas da UI

| Aba | Conteúdo |
|-----|----------|
| Dashboard | Cards-resumo, evolução mensal empilhada, aging, concentração top-5 |
| Financeiro | Rendimento, geradas×recebidas, fluxo previsto, DRE, ranking proprietários |
| Imóveis | Inadimplentes, ranking atrasos, rentabilidade, vacância |
| Contratos | Vencimentos 30/60/90, score de risco do inquilino |
| Fiscal | NFS-e emitidas/canceladas, divergências, tabela por competência |

## Parcelas na NFS-e

A partir desta fatia, `NotaFiscalServico` persiste `ValorAluguel`, `ValorIptu` e `ValorCondominio` (preenchidos em `GerarCobranca`) para o DRE e gráficos de sazonalidade. Registros legados sem parcelas usam aluguel = valor do serviço.

## Premium (incluído)

- Aging de recebíveis (0–30 / 31–60 / 61–90 / +90)
- Concentração dos 5 maiores contratos (% receita)
- Score de risco do inquilino (Baixo / Médio / Alto pela média de dias em atraso)

Rentabilidade % sobre valor venal fica para quando o cadastro do imóvel expuser esse campo.
