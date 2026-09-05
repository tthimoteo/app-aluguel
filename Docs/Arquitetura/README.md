# Arquitetura — APP Aluguel (Lucrare)

Documentação de arquitetura da plataforma **APP Aluguel**, um SaaS **multi-tenant** para gestão de locações, recebimentos, inadimplência e emissão de **NFS-e** (via **API Nacional da NFS-e**), com backend **.NET 9** e frontend **Next.js 15**, sobre **PostgreSQL** + **Supabase Storage** e integração **Mercado Pago**.

Base: análise integral de [`../APP-Aluguel-Especificacao.md`](../APP-Aluguel-Especificacao.md).

## Índice

| # | Documento | Conteúdo |
|---|---|---|
| 01 | [Arquitetura da Solução](01-Arquitetura-da-Solucao.md) | Stack oficial, estilo (Clean Architecture/DDD), componentes, estados, fluxos, RNF. |
| 02 | [Estrutura dos Projetos](02-Estrutura-dos-Projetos.md) | Solução .NET (camadas) + resumo do frontend Next.js, pacotes. |
| 03 | [Modelo PostgreSQL](03-Modelo-PostgreSQL.md) | DDL completo, índices, constraints, RLS, Identity. |
| 04 | [ERD](04-ERD.md) | Diagrama entidade-relacionamento e cardinalidades. |
| 05 | [Entidades do Entity Framework](05-Entidades-EntityFramework.md) | Entidades C#, VOs, enums, `AppUser` (Identity), `DbContext`, interceptors. |
| 06 | [Estratégia Multi-Tenant](06-Estrategia-Multi-Tenant.md) | Isolamento (coluna + RLS), resolução de tenant, limites de plano. |
| 07 | [Estratégia Supabase Storage](07-Estrategia-Supabase-Storage.md) | Buckets `contracts/certificates/nfse-xml/nfse-pdf/reports`, signed URLs, certificado A1. |
| 08 | [Estratégia de Autenticação](08-Estrategia-Autenticacao.md) | ASP.NET Identity + JWT, perfis Gestor/Analista/AdminSistema, RBAC + guarda de plano. |
| 09 | [Plano de Implementação (Sprints)](09-Plano-de-Implementacao-Sprints.md) | Incrementos alinhados ao MVP, UC e BDD; rastreabilidade e riscos. |
| 10 | [Requisitos Funcionais e Telas](10-Requisitos-Funcionais-e-Telas.md) | Home, emissão, pagamentos, histórico, menu, contabilidade, RNF. |
| 11 | [Casos de Uso e Critérios de Aceite](11-Casos-de-Uso-e-Criterios-de-Aceite.md) | UC001–UC008 e cenários BDD (CASO 1–8). |

## Stack (especificação §19)

- **Frontend**: Next.js 15 · React 19 · TypeScript · Tailwind v4 · shadcn/ui · Lucide · Recharts · next-pwa.
- **Backend**: .NET 9 Web API · EF Core · FluentValidation · JWT · **ASP.NET Identity** · QuestPDF · ClosedXML.
- **Banco/Storage**: PostgreSQL (Supabase) · Supabase Storage.
- **Pagamentos**: Mercado Pago. **NFS-e**: API Nacional da NFS-e (primeiro momento).
- **Hospedagem**: Vercel (frontend) · Render/Azure App Service (backend) · Supabase (DB + Storage).

## Decisões-chave (resumo)

- **Clean Architecture** com DDD tático e CQRS leve (MediatR).
- **Multi-tenant compartilhado** (`tenant_id`) com *global query filters* + **RLS** (segregação total, §14).
- **ASP.NET Core Identity + JWT** (perfis Gestor/Analista/AdminSistema); MFA opcional.
- **Supabase Storage** privado (5 buckets); senha do certificado A1 cifrada no backend.
- **Mercado Pago** com webhooks idempotentes (Inbox) + Outbox.
- **Worker** para ciclo de vida da assinatura e fila fiscal (emissão/cancelamento).
- Regras de negócio e **critérios de aceite (CASO 1–8)** rastreados a índices/policies.

## Diagramas

Usam **Mermaid** e renderizam no GitHub e no Cursor. Todos foram validados por render ([Mermaid Live Editor](https://mermaid.live)).
