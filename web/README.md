# APP Aluguel — Frontend (Next.js 15)

Interface web do **APP Aluguel** (Lucrare): gestão de locações, recebimentos e NFS-e.

Stack: **Next.js 15** · **React 19** · **TypeScript** · **Tailwind v4** · **shadcn/ui** · **Lucide** · **Recharts**. Identidade visual da skill `lucrare-frontend` (laranja `#DB6838`, navy `#2C3E50`).

## Pré-requisitos

- Node 22+
- API .NET em execução (`http://127.0.0.1:5272` por padrão)

## Desenvolvimento

```bash
cd web
cp .env.example .env.local   # opcional — o padrão já aponta para a API local
npm install
npm run dev
```

Abra [http://localhost:3000](http://localhost:3000). Em Development, usuários demo da API:

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador | `admin@aluguel.local` | `Admin@123456` |
| Gestor | `gestor@demo.local` | `Gestor@123456` |
| Analista | `analista@demo.local` | `Analista@123456` |

## O que está no esqueleto

- Login (`POST /api/auth/login`) com cookies httpOnly (BFF Next.js → API .NET)
- Layout com menu lateral (visibilidade por perfil, §13) e cabeçalho navy
- Tema claro/escuro (`next-themes`)
- Home com indicadores a partir de imóveis, inquilinos, contratos e planos
- Listagens: clientes, imóveis, inquilinos, contratos, usuários
- Minha Conta (Gestor) com `/api/auth/me` e catálogo de planos
- Placeholders: relatórios, dados para contabilidade, auditoria

## Testes

```bash
npm test    # Vitest (menu por perfil, formatação)
npm run lint
npm run build
```

## Variáveis

| Variável | Uso |
|---|---|
| `API_URL` | Base da API .NET (somente servidor). Padrão: `http://127.0.0.1:5272` |
