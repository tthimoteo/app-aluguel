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

## Módulo Clientes (CRUD)

CRUD de clientes (PF/PJ) em CQRS (MediatR), com validação, isolamento multi-tenant e DTOs.

Endpoints (`/api/clientes`, exigem autenticação):

- `GET /api/clientes?termo=&tipo=PF|PJ&skip=&take=` — lista paginada do tenant (filtro por nome/CPF/CNPJ).
- `GET /api/clientes/{id}` — obtém por id (404 se não existir no tenant).
- `POST /api/clientes` — cria PF ou PJ (201). Requer política `GerenciaUsuarios` (Administrador/Gestor).
- `PUT /api/clientes/{id}` — atualiza dados cadastrais (tipo e CPF/CNPJ são imutáveis). Requer `GerenciaUsuarios`.
- `DELETE /api/clientes/{id}` — exclusão lógica (soft delete, 204). Requer `GerenciaUsuarios`.

Características:

- **Multi-tenant**: o `TenantId` vem do JWT (nunca do corpo). Leituras/gravações passam pelos filtros globais de tenant e soft delete do `AppDbContext`; a unicidade de CPF/CNPJ é por tenant.
- **Validações** (FluentValidation, executadas por `ValidationBehavior` no pipeline do MediatR): dígitos verificadores de CPF/CNPJ, obrigatoriedades por tipo de pessoa, unicidade de CPF/CNPJ, e-mail, UF (2), CEP (8 dígitos). Máscaras de CPF/CNPJ/CEP são normalizadas. Erros retornam `400` em formato ProblemDetails (via `ExceptionHandlingMiddleware`).
- **DTOs**: `ClienteDto`/`EnderecoDto` (resposta) e `EnderecoInput` (entrada); listagem em `PaginaDto<T>`.
- **Swagger**: os cinco endpoints aparecem no grupo **Clientes**; enums são serializados como texto.

## Módulo Usuários (CRUD)

CRUD dos **usuários do cliente** (§6) em CQRS (MediatR), sobre o ASP.NET Identity (senha via `PasswordHasher`, role atribuída pelo `UserManager`). O usuário é o `AppUser` (tabela `identity.asp_net_users`) — nenhuma migration nova é necessária.

Endpoints (`/api/usuarios`, exigem a política `GerenciaUsuarios` = **Administrador** ou **Gestor**):

- `GET /api/usuarios?clienteId=&termo=&skip=&take=` — lista paginada (filtro por nome/e-mail). O Gestor lista apenas o próprio cliente; o Administrador informa `clienteId`.
- `GET /api/usuarios/{id}` — obtém por id (404 fora do escopo do cliente).
- `POST /api/usuarios` — cria usuário Gestor/Analista (201). O cliente alvo é o do Gestor; o Administrador informa `clienteId` no corpo.
- `PUT /api/usuarios/{id}` — atualiza nome, e-mail, telefone, perfil e status (CPF é imutável).
- `DELETE /api/usuarios/{id}` — desativação lógica (`Status = Inativo`, 204) preservando o histórico/auditoria.

Regras da documentação implementadas:

- **Limite por plano (UC002)**: a criação (e a reativação) respeita `plano.max_usuarios` do cliente — contam os usuários **Ativos**. Excedente retorna `400`.
- **CPF único por cliente (CASO 1)**: além do índice único parcial `ux_usuario_cpf`, validação em `FluentValidation` com dígitos verificadores.
- **Perfis Gestor e Analista (§6)**: este módulo só provisiona/edita `Gestor`/`Analista`; o perfil vira role do Identity. `Administrador` é papel de plataforma e não é criado aqui.
- **Multi-tenant / isolamento (§6)**: leituras escopadas pelo `tenant_id` do JWT; um Gestor só enxerga/gerencia usuários do seu próprio cliente (fora do escopo → `404`). Não é permitido remover o próprio usuário.
- **Status** `Ativo`/`Inativo`/`Bloqueado`: inativo/bloqueado não autentica (validado no login).

Validações (FluentValidation, via `ValidationBehavior`): e-mail obrigatório/único/formato, senha ≥ 10, perfil ∈ {Gestor, Analista}, CPF (quando informado) válido e único no cliente, limite do plano. Erros retornam `400` em ProblemDetails.

## Módulos Imóveis, Inquilinos e Contratos (CRUD + endpoints REST)

Cadastros operacionais (§7, §8, §9) em CQRS (MediatR), com repositórios EF Core escopados por tenant/soft delete. As tabelas `imovel`, `inquilino` e `contrato` já existem na migration `AddDomainModel` — **nenhuma migration nova**.

Leitura liberada a qualquer perfil autenticado (o **Analista** consulta); escrita e transições de estado exigem a política `GerenciaCadastros` = **Administrador** ou **Gestor**. Não-admins operam apenas no próprio cliente (fora do escopo → `404`); o Administrador informa `clienteId`.

**Imóveis** (`/api/imoveis`): `GET /` (filtros `clienteId`, `termo`, `tipo`, `status`, paginação), `GET /{id}`, `POST /`, `PUT /{id}` (inclui `status` Ativo/Inativo), `DELETE /{id}` (exclusão lógica).

**Inquilinos** (`/api/inquilinos`): `GET /` (filtros `clienteId`, `termo`, `tipoPessoa`, `status`), `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}` (exclusão lógica). Tipo de pessoa e documento são imutáveis.

**Contratos** (`/api/contratos`): `GET /` (filtros `clienteId`, `imovelId`, `inquilinoId`, `status`), `GET /{id}`, `POST /`, `PUT /{id}` (cláusulas, só enquanto Ativo), `POST /{id}/encerrar`, `POST /{id}/cancelar`. Não há `DELETE`: o contrato não é *soft-deletable*; o término se dá por Encerrar/Cancelar (Ativo → Encerrado/Cancelado), preservando o histórico.

Regras da documentação implementadas:

- **Limite de imóveis por plano (UC003)**: a criação (e a reativação de um imóvel inativo) respeita `plano.max_imoveis` — contam apenas imóveis **Ativos** (inativar libera vaga, alinhado ao CASO 2 de downgrade). Excedente retorna `400`.
- **CASO 3 — 1 contrato ativo por imóvel**: validado na aplicação e reforçado pelo índice único parcial `ux_contrato_imovel_ativo`. Encerrar/cancelar libera o imóvel para novo contrato.
- **Mesmo cliente**: imóvel e inquilino de um contrato devem pertencer ao mesmo cliente; o `clienteId` do contrato é derivado do imóvel.
- **Exclusão só sem dependentes (§4)**: não remove imóvel/inquilino com contrato ativo (`400`).
- **Validações** (FluentValidation): tamanhos conforme DDL, `tipo`/`status` válidos, CPF/CNPJ do inquilino (dígitos verificadores) por tipo de pessoa, `diaVencimento` 1..31, `valorAluguel` > 0, `dataFimPrevista` ≥ `dataInicio`, CEP com 8 dígitos.

## Endpoints atuais

- `GET /health` — verificação de saúde.
- `GET /api/planos` — catálogo de planos (seed conforme especificação §3).
- `GET /swagger` — documentação interativa com botão **Authorize** (Bearer JWT), ambiente Development.
- CORS (`Cors:Origins`) — origens do frontend Next.js (`http://localhost:3000` em desenvolvimento).

## Testes

```bash
dotnet build Aluguel.sln
dotnet test Aluguel.sln
```

## Estado

Sprint 0 (fundação) + fatia vertical de **Planos** + **modelo de domínio completo (entidades EF, relacionamentos, `TenantId`, `DbContext`, migrations)** + **autenticação (ASP.NET Identity + JWT + refresh token + RBAC)** + **módulo Clientes (CRUD + validações + multi-tenant)** + **módulo Usuários (CRUD + limites por plano + perfis Gestor/Analista + isolamento)** + **módulos Imóveis/Inquilinos/Contratos (CRUD + endpoints REST + limite por plano + CASO 3 + dependentes)** entregues. Próximos incrementos seguem o plano de sprints (assinatura/Mercado Pago, NFS-e, financeiro, etc.).
