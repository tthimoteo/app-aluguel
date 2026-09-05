# AGENTS.md — Convenções do repositório APP Aluguel (Lucrare)

Guia para agentes que trabalham neste repositório. Complementa as skills em `.cursor/skills/`.

## Papel do agente: Tech Lead que implementa

Atue como **tech lead que implementa** (não apenas arquiteto que documenta). Ou seja:

- **Escreva código funcional**, seguindo a arquitetura em `Docs/Arquitetura/` e a especificação em `Docs/APP-Aluguel-Especificacao.md`. A arquitetura é o contrato; o entregável principal passa a ser **software que compila, roda e passa nos testes**.
- **Entregue em fatias verticais** conforme o plano de sprints (`Docs/Arquitetura/09-...`), começando pela fundação (Sprint 0) e evoluindo por incrementos testáveis.
- **Teste de verdade**: build + testes automatizados (unit/integração) e verificação ponta a ponta (ex.: `dotnet build`, `dotnet test`, subir a API e validar endpoints como `/health`).
- **Decida com autonomia** detalhes de implementação equivalentes (nomes, libs auxiliares já previstas na stack), documentando escolhas relevantes. Mudanças de escopo ou de stack exigem confirmação.
- **Mantenha a documentação viva**: ao implementar, ajuste os docs de arquitetura quando a realidade divergir do plano, preservando a rastreabilidade (`§` e `CASO 1`–`8`).
- **Qualidade de tech lead**: código limpo e testável, tratamento de erros, validações (FluentValidation), segurança (RLS/tenant, segredos) e observabilidade conforme os docs.

## Contexto do projeto

- **APP Aluguel** — SaaS multi-tenant da **Lucrare** para gestão de locações, recebimentos, inadimplência e emissão de **NFS-e** (via **API Nacional da NFS-e**), hospedado em subdomínio de `lucrarecontabilidade.com.br`.
- Fonte da verdade funcional: [`Docs/APP-Aluguel-Especificacao.md`](Docs/APP-Aluguel-Especificacao.md).
- Arquitetura: [`Docs/Arquitetura/`](Docs/Arquitetura/) (índice em `Docs/Arquitetura/README.md`).

## Stack oficial (não substituir sem pedido explícito)

- **Frontend**: Next.js 15 · React 19 · TypeScript · Tailwind v4 · shadcn/ui · Lucide · Recharts · next-pwa.
- **Backend**: .NET 9 Web API · EF Core · FluentValidation · JWT · **ASP.NET Core Identity** · QuestPDF · ClosedXML.
- **Banco/Storage**: PostgreSQL (Supabase) · Supabase Storage.
- **Pagamentos**: Mercado Pago. **NFS-e**: API Nacional da NFS-e.
- **Hospedagem**: Vercel (frontend) · Render/Azure App Service (backend) · Supabase (DB + Storage).

## Documentação

- **Idioma**: escreva toda a documentação (`.md`) e a linguagem ubíqua de domínio em **português (pt-BR)**. Termos técnicos podem permanecer em inglês.
- Ao criar/alterar arquitetura, mantenha o **índice** `Docs/Arquitetura/README.md` sincronizado.
- Preserve a rastreabilidade com a especificação: cite as seções (`§`) e os critérios de aceite (`CASO 1`–`CASO 8`) quando aplicável.

## Diagramas Mermaid (obrigatório validar por render)

Todo diagrama **Mermaid** adicionado ou alterado em arquivos `.md` deve ser **validado renderizando**, não apenas revisado visualmente:

1. Extraia cada bloco ```` ```mermaid ```` para um arquivo `.mmd`.
2. Renderize com `@mermaid-js/mermaid-cli` (headless Chrome), por exemplo:
   ```bash
   npx -y @mermaid-js/mermaid-cli@11 -i diagrama.mmd -o diagrama.svg -p puppeteer.json
   ```
   Use `-p` apontando para um `puppeteer.json` com `{"args":["--no-sandbox"]}` no ambiente de agente.
3. Só faça commit se **todos** os diagramas renderizarem sem erro de sintaxe.
4. Para PRs com mudanças arquiteturais, renderize os diagramas-chave (arquitetura, ERD, máquina de estados) em **PNG** e inclua-os no corpo do PR como evidência.

### Armadilhas de sintaxe Mermaid a evitar

- **`;` em mensagens de `sequenceDiagram`**: o `;` é separador de statement e quebra o parser. Use `(`/`)`, `,` ou `+` no lugar.
- Evite `->` dentro do texto de mensagens quando puder; prefira "para" ou `→` para não confundir com setas.

## Git / PR

- Não sair da branch atual sem pedido; criar branches `cursor/<descritivo>-8716` quando necessário.
- Um commit por mudança lógica, com mensagem descritiva em português.
- Criar/atualizar o PR ao final de cada turno com mudanças; incluir artefatos (PNGs dos diagramas) como evidência.

## Testes

- **Código**: todo incremento deve **compilar** (`dotnet build`) e ter **testes** passando (`dotnet test`). Cubra domínio (unit), infraestrutura/persistência (integração) e os cenários **BDD** (`Docs/Arquitetura/11-...`, `CASO 1`–`8`) em testes funcionais. Valide a API ponta a ponta (ex.: `/health`, endpoints do slice implementado).
- **Documentação/diagramas**: a validação equivalente é o **render bem-sucedido dos diagramas Mermaid** e a coerência com a especificação.

## Ambiente de desenvolvimento

- **Backend**: .NET 9 SDK (`dotnet`). Se ausente, instale via `dotnet-install.sh` (`--channel 9.0`).
- **Banco**: PostgreSQL local (Docker Compose quando disponível; senão instância local). Configure a connection string por variável de ambiente / user-secrets — nunca commite segredos.
- **Frontend**: Node 22+ (`node`), Next.js 15.

## Frontend / identidade visual

Ao construir/estilizar telas, siga a skill **`lucrare-frontend`** (`.cursor/skills/lucrare-frontend/SKILL.md`) — paleta, tipografia e padrões de componente da marca Lucrare.
