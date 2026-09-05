# Lucrare — APP Aluguel + Skill de Frontend

Este repositório contém:

1. **APP Aluguel** — aplicação web (React + TypeScript + Vite) para gestão de locações e recebimentos, estilizada com a identidade visual da Lucrare. É o ponto de partida para a plataforma descrita em `Docs/APP-Aluguel-Especificacao.md`.
2. Uma **Cursor Agent Skill** de frontend baseada na identidade visual e nos padrões de UI reais da marca **Lucrare**, uma contabilidade estratégica de São Paulo:

- **Site institucional**: [lucrarecontabilidade.com.br](https://www.lucrarecontabilidade.com.br/)
- **Lucrare Gestão** (app de gestão de clientes/usuários): [lucraregestao.netlify.app](https://lucraregestao.netlify.app/login)

## Desenvolvimento (APP Aluguel)

Pré-requisitos: Node.js 22+.

```bash
npm ci        # instala as dependências
npm run dev   # sobe o servidor de desenvolvimento em http://localhost:5173
npm run build # build de produção (typecheck + Vite)
npm run lint  # oxlint
```

O app usa dados em memória (seed) para demonstração — faça login com qualquer usuário/senha (sugestão: `admin` / `lucrare`). Módulos: Dashboard, Imóveis, Inquilinos e Recebimentos.

O ambiente de Cloud Agent é configurado em `.cursor/environment.json` (`install: npm ci`, terminal `dev` rodando `npm run dev`, porta `5173`).

## O que tem aqui

```
.cursor/skills/lucrare-frontend/
├── SKILL.md                       # visão geral da marca e diretrizes de implementação
└── references/
    ├── design-tokens.md           # paleta de cores, tipografia, raio, sombra, tokens CSS/Tailwind
    └── component-patterns.md      # exemplos de código: login, header, tabelas → cards, badges, modal, formulários
```

Os tokens e padrões foram extraídos diretamente do CSS de produção do Lucrare Gestão e do HTML renderizado do site institucional (paleta de cores, fontes, raios de borda, sombras, breakpoints), não são estimativas genéricas.

## Como usar

Esta skill é descoberta automaticamente pelo Cursor Agent (IDE, CLI e Cloud Agents) sempre que o repositório contém `.cursor/skills/`. Basta pedir para o agente construir, estilizar ou revisar uma tela/componente que deva seguir a identidade da Lucrare (ex.: "cria uma tela de cadastro de clientes no estilo da Lucrare") e o agente vai ler `SKILL.md` e aplicar os tokens e padrões documentados automaticamente.

Você também pode invocar manualmente digitando `/lucrare-frontend` no chat, ou referenciar com `@lucrare-frontend`.

## Resumo da identidade visual

- **Cor de destaque (accent)**: laranja terracota `#DB6838` — botões primários, links, foco de campos, badges de destaque.
- **Cor de autoridade**: azul-marinho escuro `#2C3E50` — cabeçalhos e títulos.
- **Fundo**: tons neutros claros (`#F8F9FA`, `#F5F5F5`).
- **Tipografia**: stack de fontes de sistema no app (utilitário); Helvetica/Montserrat Bold nos títulos do site institucional.
- **Padrão responsivo**: tabelas de dados em desktop se transformam em cards empilhados no mobile (breakpoint 768px).

Veja `.cursor/skills/lucrare-frontend/SKILL.md` para o guia completo.
