# APP Aluguel (Lucrare)

SaaS multi-tenant da **Lucrare** para gestão de locações, recebimentos, inadimplência e emissão de **NFS-e**. Backend **.NET 9** e frontend **Next.js 15**, sobre PostgreSQL.

- Especificação: [`Docs/APP-Aluguel-Especificacao.md`](Docs/APP-Aluguel-Especificacao.md)
- Arquitetura: [`Docs/Arquitetura/README.md`](Docs/Arquitetura/README.md)
- Backend: [`src/README.md`](src/README.md)
- Frontend: [`web/README.md`](web/README.md)

## Stack

| Camada | Tecnologia |
|---|---|
| Frontend | Next.js 15 · React 19 · TypeScript · Tailwind v4 · shadcn/ui |
| Backend | .NET 9 Web API · EF Core · FluentValidation · JWT · ASP.NET Identity |
| Banco | PostgreSQL (Docker Compose local / Supabase) |

Identidade visual: skill [`.cursor/skills/lucrare-frontend`](.cursor/skills/lucrare-frontend/SKILL.md) (laranja `#DB6838`, navy `#2C3E50`).

## Rodando localmente

```bash
docker compose up -d postgres
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Aluguel.Api
# em outro terminal
cd web && npm install && npm run dev
```

API: `http://localhost:5272` · App: `http://localhost:3000` · login demo: `gestor@demo.local` / `Gestor@123456`.
