# APP Aluguel — Backend (.NET 9)

Implementação do backend seguindo a arquitetura em [`../Docs/Arquitetura`](../Docs/Arquitetura). Solução em **Clean Architecture** com **CQRS leve (MediatR)**, **EF Core + PostgreSQL** e **ASP.NET Identity**.

## Projetos

| Projeto | Papel |
|---|---|
| `Aluguel.Domain` | Entidades, value objects, enums, tipos base (sem dependências externas). |
| `Aluguel.Application` | Casos de uso (CQRS/MediatR), ports (abstrações), validações. |
| `Aluguel.Infrastructure` | EF Core + Identity, `AppDbContext`, migrations, repositórios. |
| `Aluguel.Api` | Web API (endpoints, health, Swagger). |
| `tests/Aluguel.Domain.UnitTests` | Testes de unidade do domínio. |

## Rodando localmente

Pré-requisitos: .NET 9 SDK e PostgreSQL (via `docker compose up -d` na raiz, ou instância local).

```bash
# Banco (opção Docker)
docker compose up -d postgres

# Migrations
dotnet ef database update -p src/Aluguel.Infrastructure -s src/Aluguel.Api

# API
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Aluguel.Api
```

Connection string em `src/Aluguel.Api/appsettings.json` (`ConnectionStrings:Postgres`) — sobrescreva por variável de ambiente/secret em ambientes reais. A senha padrão é apenas para desenvolvimento local.

## Endpoints atuais (Sprint 0)

- `GET /health` — verificação de saúde.
- `GET /api/planos` — catálogo de planos (seed conforme especificação §3).
- `GET /swagger` — documentação interativa (ambiente Development).

## Testes

```bash
dotnet build Aluguel.sln
dotnet test Aluguel.sln
```

## Estado

Sprint 0 (fundação) + fatia vertical de **Planos** entregues. Próximos incrementos seguem o plano de sprints (auth/tenant, imóveis/contratos, assinatura/Mercado Pago, NFS-e, etc.).
