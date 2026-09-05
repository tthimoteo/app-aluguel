# Arquitetura — APP Aluguel (Lucrare)

Documentação de arquitetura da plataforma **APP Aluguel**, um SaaS **multi-tenant** em **.NET 9** para gestão de locações, recebimentos, inadimplência e emissão de **NFS-e**, com **PostgreSQL** (Supabase), **Supabase Storage** e integração **Mercado Pago**.

Base: análise integral de [`../APP-Aluguel-Especificacao.md`](../APP-Aluguel-Especificacao.md).

## Índice

| # | Documento | Conteúdo |
|---|---|---|
| 01 | [Arquitetura da Solução](01-Arquitetura-da-Solucao.md) | Estilo (Clean Architecture/DDD), componentes, máquina de estados, fluxos, stack. |
| 02 | [Estrutura dos Projetos](02-Estrutura-dos-Projetos.md) | Organização da solução .NET, dependências entre camadas, convenções. |
| 03 | [Modelo PostgreSQL](03-Modelo-PostgreSQL.md) | DDL completo, índices, constraints, triggers, RLS. |
| 04 | [ERD](04-ERD.md) | Diagrama entidade-relacionamento e cardinalidades. |
| 05 | [Entidades do Entity Framework](05-Entidades-EntityFramework.md) | Entidades C#, VOs, enums, `DbContext`, configurations, interceptors. |
| 06 | [Estratégia Multi-Tenant](06-Estrategia-Multi-Tenant.md) | Isolamento (coluna + RLS), resolução de tenant, limites de plano. |
| 07 | [Estratégia Supabase Storage](07-Estrategia-Supabase-Storage.md) | Buckets, paths por tenant, signed URLs, segurança do certificado A1. |
| 08 | [Estratégia de Autenticação](08-Estrategia-Autenticacao.md) | Supabase Auth (JWT/JWKS), claims multi-tenant, RBAC + guarda de plano. |
| 09 | [Plano de Implementação (Sprints)](09-Plano-de-Implementacao-Sprints.md) | Incrementos verticais, DoD, rastreabilidade e riscos. |

## Decisões-chave (resumo)

- **Clean Architecture** com DDD tático e CQRS leve (MediatR).
- **.NET 9 + EF Core 9 + Npgsql** sobre **PostgreSQL 16** (Supabase).
- **Multi-tenant compartilhado** (`tenant_id`) com defesa em profundidade: *global query filters* + **RLS**.
- **Supabase Auth (GoTrue)** como IdP (JWT com `tenant_id`), validado *stateless* pela API.
- **Supabase Storage** privado para XML/PDF de NFS-e e certificados A1; senha do certificado cifrada no backend.
- **Mercado Pago** para assinaturas, com **webhooks idempotentes** (Inbox) e **Outbox** para eventos.
- **Worker** dedicado para jobs de ciclo de vida (trial, tolerância, suspensão, reativação) e fila fiscal.

## Como visualizar os diagramas

Os diagramas usam **Mermaid** e renderizam diretamente no GitHub e no Cursor. Em outros editores, use uma extensão Mermaid ou o [Mermaid Live Editor](https://mermaid.live).
