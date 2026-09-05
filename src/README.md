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

## Modelo de dados (entidades EF)

Modelo de domínio completo mapeado no `AppDbContext` (schema `app`; Identity no schema `identity`):

`Tenant`, `Plano`, `Cliente` (PF/PJ, `Endereco` owned, soft delete), `AppUser`/`AppRole` (Usuário via ASP.NET Identity), `CertificadoDigital`, `Assinatura`, `PagamentoPlano`, `AuditoriaAssinatura`, `Imovel`, `Inquilino`, `Contrato`, `NotaFiscalServico` (NFS-e), `DocumentoFiscal`, `Pagamento`, `Despesa`, `AuditLog`.

Pontos de modelagem:

- **`TenantId`** em todas as entidades de negócio (`ITenantOwned`) + **filtro global** de tenant e de soft delete (`ISoftDeletable`) aplicados automaticamente no `OnModelCreating`. A resolução do tenant (`ICurrentTenant`) usa um provider nulo por ora; a versão baseada na claim `tenant_id` do JWT entra junto da autenticação.
- **Enums** persistidos como texto; monetários em `numeric(14,2)`; percentuais em `numeric(6,3)`; competência em `char(7)` (`MM/AAAA`).
- **Índices únicos parciais** que materializam as regras de negócio: CPF/CNPJ por tenant (CASO 1), 1 contrato ativo por imóvel (CASO 3), 1 NFS-e não-cancelada por imóvel/competência e 1 pagamento por imóvel/competência (CASO 6/7/8), assinatura vigente única por cliente, `mercado_pago_payment_id` único.
- **Relacionamentos** configurados via `IEntityTypeConfiguration<T>` com `ON DELETE RESTRICT` (exceto coleções filhas em cascade: `Assinatura→PagamentoPlano`, `NFS-e→DocumentoFiscal`). FK cross-schema `asp_net_users → app.cliente`.

Migrations: `InitialCreate` (Tenant/Plano/Identity) e `AddDomainModel` (demais entidades).

> Controllers/endpoints de CRUD ainda **não** implementados nesta etapa (apenas o modelo, o `DbContext` e as migrations).

## Autenticação (ASP.NET Identity + JWT)

Autenticação própria com ASP.NET Core Identity emitindo **access token JWT** (HS256, curto) + **refresh token rotativo** persistido/revogável em `identity.refresh_token` (guarda-se apenas o hash SHA-256).

Roles (RBAC): **Administrador**, **Gestor**, **Analista** (coincidem com `PerfilUsuario`). O JWT carrega `sub`, `email`, `name`, `tenant_id`, `cliente_id` e `role` — o `tenant_id` alimenta o filtro global multi-tenant do EF.

Endpoints:

- `POST /api/auth/login` — `{ email, senha }` → access/refresh + dados do usuário.
- `POST /api/auth/refresh` — `{ refreshToken }` → rotaciona o refresh e emite novo access (reuso do antigo é bloqueado).
- `POST /api/auth/logout` — `{ refreshToken }` (requer Bearer) → revoga o refresh (204).
- `GET /api/auth/me` — claims do usuário autenticado.

Autorização por role/política (`Program.cs`): `GerenciaUsuarios` (Administrador, Gestor), `EmitirNfse`/`CancelarNfse` (Administrador, Gestor, Analista). Endpoints de diagnóstico: `GET /api/admin/ping`, `/api/gestao/ping`, `/api/nfse/ping`.

Config em `appsettings.json` → seção `Jwt` (`Issuer`, `Audience`, `SigningKey`, `AccessTokenMinutes`, `RefreshTokenDays`). **A `SigningKey` do repositório é apenas para desenvolvimento** — use secret/variável de ambiente em ambientes reais.

Em **Development**, o startup cria roles e usuários demo para testes: `admin@aluguel.local` / `Admin@123456` (Administrador), `gestor@demo.local` / `Gestor@123456` (Gestor), `analista@demo.local` / `Analista@123456` (Analista).

## Endpoints atuais

- `GET /health` — verificação de saúde.
- `GET /api/planos` — catálogo de planos (seed conforme especificação §3).
- `GET /swagger` — documentação interativa com botão **Authorize** (Bearer JWT), ambiente Development.

## Testes

```bash
dotnet build Aluguel.sln
dotnet test Aluguel.sln
```

## Estado

Sprint 0 (fundação) + fatia vertical de **Planos** + **modelo de domínio completo (entidades EF, relacionamentos, `TenantId`, `DbContext`, migrations)** + **autenticação (ASP.NET Identity + JWT + refresh token + RBAC)** entregues. Próximos incrementos seguem o plano de sprints (casos de uso de clientes/usuários, imóveis/contratos, assinatura/Mercado Pago, NFS-e, etc.).
