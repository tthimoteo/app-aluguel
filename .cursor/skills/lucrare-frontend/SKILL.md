---
name: lucrare-frontend
description: Design system and UI/UX conventions of Lucrare — a Brazilian accounting firm — covering both its institutional marketing site (lucrarecontabilidade.com.br) and its internal client/user management dashboard (lucraregestao.netlify.app). Use whenever building, styling, or reviewing frontend UI (landing pages, dashboards, forms, tables, auth screens, modals) that should look and feel consistent with the Lucrare brand: color palette, typography, spacing, border radius, shadows, and concrete component patterns (buttons, cards, tables, badges, navigation, login forms).
---

# Lucrare — Frontend Design System

Esta skill documenta a identidade visual e os padrões de UI da **Lucrare**, extraídos e catalogados a partir de dois produtos reais da marca:

1. **Site institucional** — [lucrarecontabilidade.com.br](https://www.lucrarecontabilidade.com.br/) — site de marketing (Wix) de uma contabilidade estratégica em São Paulo (Brás), com +30 anos de mercado e +300 clientes.
2. **Lucrare Gestão** — [lucraregestao.netlify.app](https://lucraregestao.netlify.app/login) — aplicação interna (React/CRA) de gestão de clientes e usuários, com tela de login, dashboard, tabelas de clientes/usuários, comentários e badges de status.

Use esta skill sempre que for criar ou ajustar telas, componentes ou estilos que precisem manter consistência com a marca Lucrare — seja um novo módulo do painel de gestão, uma landing page institucional, ou qualquer tela auxiliar (login, formulários, relatórios).

Consulte `references/design-tokens.md` para a paleta de cores, tipografia, espaçamento e sombras em formato de tokens (CSS vars / Tailwind), e `references/component-patterns.md` para exemplos de código prontos (botões, cards, tabelas, modais, login, badges, navegação).

## Identidade da marca

- **Tom**: profissional, sólido, confiável — "contabilidade estratégica" para empresários do varejo (forte presença no Brás/SP), foco em segurança patrimonial e tomada de decisão.
- **Copy real usada no site**: "Somos a Lucrare", "Contabilidade estratégica", "+300 Clientes", "+30 Anos de Experiência", "Cada cliente é único!". Evite copy genérica tipo "Welcome" — escreva em PT-BR, direto, com foco em resultado e segurança fiscal/tributária.
- **Cor de destaque (accent)**: laranja terracota `#DB6838` (marketing usa a mesma família de laranja: `#DD6112`, `#ff8044`). É a cor de CTA, links, foco de inputs, estados ativos e badges de destaque em **ambos** os produtos — é o elemento mais consistente da marca entre o site e o app.
- **Cor de autoridade (headers/títulos)**: azul-marinho/ardósia escuro `#2C3E50` (app) e azul profundo `#02356E` (site institucional). Use tons escuros e neutros para cabeçalhos, nunca preto puro.
- **Base neutra**: fundos claros e levemente acinzentados (`#F8F9FA`, `#F5F5F5`, `#F9F9F5`), cards brancos, bordas cinza-claro (`#E9ECEF`, `#DDD`, `#ECF0F1`).

## Diretrizes rápidas de implementação

1. **Tipografia**: use uma stack de fontes de sistema/sans-serif (ex.: `-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif`) para o app/dashboard (utilitário, rápido, neutro). No site institucional/marketing, títulos podem ser mais pesados (bold/black) como Helvetica Bold ou Montserrat Black para reforçar autoridade; corpo de texto em Helvetica/Arial regular.
2. **Cor primária de ação = laranja `#DB6838`**. Todo botão primário, link, estado de foco (`border-color` + `box-shadow` sutil na mesma cor) e badge de destaque deve usar essa cor (ou seu hover mais escuro `#C55A32`/`#C55A2F`). Não introduza uma cor de accent diferente sem necessidade explícita.
3. **Cantos arredondados moderados, nunca "pill" excessivo em botões**: `4px` para inputs/botões, `8px` para cards e modais, `12px` para badges/pills de status e cards de listagem mobile.
4. **Sombras discretas** — nunca sombras fortes ou coloridas. Cards em repouso: `0 2px 8px rgba(0,0,0,.1)`; ao passar o mouse, elevar levemente (`translateY(-2px)`) e aumentar sombra para `0 4px 12px rgba(0,0,0,.15)`. Modais: `0 10px 25px rgba(0,0,0,.3)`.
5. **Layout responsivo com breakpoint em 768px**: em telas de gestão/dashboard, tabelas densas em desktop devem se transformar em **cards verticais empilhados** no mobile (não apenas scroll horizontal) — esse é um padrão explícito já usado no produto real.
6. **Estados semânticos padronizados**: sucesso = verde (`#27AE60`/`#28A745`), erro/exclusão = vermelho (`#E74C3C`/`#DC3545`), aviso = amarelo (`#FFC107`), informação/ação secundária = azul (`#007BFF`/`#17A2B8`), neutro/cancelar = cinza (`#6C757D`/`#95A5A6`). Não invente novas cores semânticas.
7. **Telas de autenticação (login)**: card branco centralizado verticalmente e horizontalmente, largura máxima ~400px, fundo da página cinza-claro, logo centralizado acima do título, inputs full-width com foco laranja, botão de submit full-width laranja solid.
8. Sempre escreva a interface em **português do Brasil**, com terminologia contábil/administrativa coerente (ex.: "Clientes", "Honorário", "Status", "Usuário", "Senha", "Entrar", "Cadastrar", "Editar", "Excluir").

## Quando NÃO aplicar esta skill

Não force esses tokens em projetos que não tenham relação com a marca Lucrare ou que o usuário explicitamente peça uma identidade visual diferente. Se o pedido for genérico ("cria um dashboard qualquer"), pergunte-se se o contexto é sobre a Lucrare antes de aplicar cores/tom de marca específicos.
