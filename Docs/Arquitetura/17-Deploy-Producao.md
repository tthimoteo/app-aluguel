# 17 — Deploy em produção (Render + Vercel + domínio Lucrare)

Publicação do **APP Aluguel** na web, alinhada à stack oficial (§19 / `01-Arquitetura-da-Solucao.md`):

| Camada | Serviço | URL sugerida |
|---|---|---|
| Frontend | **Vercel** (Next.js 15) | `https://aluguel.lucrarecontabilidade.com.br` |
| API | **Render** (Docker .NET 9) | `https://api.aluguel.lucrarecontabilidade.com.br` |
| Banco | **Render Postgres** (MVP) ou Supabase | interno / connection string |
| E-mail | Microsoft Graph (`Mail.Send`) | já configurado no Entra |

Arquivos no repositório:

- `Dockerfile` — build da API
- `render.yaml` — Blueprint Render (API + Postgres)
- `web/vercel.json` — região `gru1` (São Paulo)

---

## 1. O que o agente já preparou no código

- Imagem Docker da API com health em `/health`
- Blueprint Render com variáveis tipadas (`Jwt__*`, `Email__*`, `Cors__Origins__*`)
- `appsettings.Production.json` (sem segredos)
- Migração EF automática no startup (`Database__MigrateOnStartup=true`)
- PIX / NFS-e / Boleto em **Mock** até você habilitar produção

## 2. O que só você consegue fazer (checklist)

Faça nesta ordem. Marque cada item.

### A) Conta Render (`tthimoteoconsultoria@gmail.com`)

1. Acesse [https://dashboard.render.com](https://dashboard.render.com) e entre com o e-mail acima.
2. **New → Blueprint** → conecte o repositório GitHub `thiago-thimoteo/app-aluguel` (ou o mirror Origin, se o Render estiver ligado a ele).
3. Selecione a branch `main` (após merge deste PR de deploy) e confirme o `render.yaml`.
4. Quando o Blueprint pedir secrets (`sync: false`), preencha:

| Variável | Como obter |
|---|---|
| `Jwt__SigningKey` | No terminal: `openssl rand -base64 48` (mín. 32 caracteres) |
| `Email__GraphTenantId` | Entra ID → Overview → Tenant ID |
| `Email__GraphClientId` | App registration já usada no Graph |
| `Email__GraphClientSecret` | Client secret do mesmo app |

5. Aguarde o deploy da API. Teste: `https://<serviço>.onrender.com/health` → `{"status":"healthy",...}`.
6. **Custom Domain** no serviço `aluguel-api`:
   - Domínio: `api.aluguel.lucrarecontabilidade.com.br`
   - Render mostra um CNAME (ex.: `aluguel-api.onrender.com`) — use na etapa DNS.

> Plano Free do Render “dorme” após inatividade (~15 min). Para produção real use pelo menos **Starter**. O Postgres Free também tem limites; migre para paid ou Supabase quando crescer.

### B) Conta Vercel (frontend)

1. Crie/entre em [https://vercel.com](https://vercel.com) (pode usar o mesmo Google `tthimoteoconsultoria@gmail.com`).
2. **Add New Project** → importe `thiago-thimoteo/app-aluguel`.
3. Configure:
   - **Root Directory**: `web`
   - **Framework**: Next.js
   - **Build Command**: `npm run build` (padrão)
   - **Install Command**: `npm install`
4. Environment Variables (Production + Preview):

| Nome | Valor |
|---|---|
| `API_URL` | `https://api.aluguel.lucrarecontabilidade.com.br` (ou a URL `.onrender.com` temporária) |

5. Deploy. Anote a URL `*.vercel.app`.
6. **Settings → Domains** → adicione `aluguel.lucrarecontabilidade.com.br`.
7. Atualize no Render a env `Cors__Origins__0` / `__1` com as URLs finais do frontend e faça **Manual Deploy** da API.

### C) DNS em `lucrarecontabilidade.com.br`

No painel do registrador / DNS do domínio (Registro.br, Cloudflare, Hostinger, etc.):

| Tipo | Nome (host) | Valor | TTL |
|---|---|---|---|
| CNAME | `aluguel` | valor que a **Vercel** mostrar (ex. `cname.vercel-dns.com`) | 300–3600 |
| CNAME | `api.aluguel` | valor que o **Render** mostrar (ex. `aluguel-api.onrender.com`) | 300–3600 |

Se o provedor não aceitar CNAME em subdomínio composto `api.aluguel`, use:

| Tipo | Nome | Valor |
|---|---|---|
| CNAME | `aluguel-api` | `aluguel-api.onrender.com` |

e ajuste o custom domain da API para `aluguel-api.lucrarecontabilidade.com.br` (atualize `API_URL` na Vercel).

Aguarde a propagação DNS (minutos a algumas horas). HTTPS é emitido automaticamente pela Vercel e pelo Render após o domínio validar.

### D) Validação ponta a ponta

1. `https://api.aluguel.lucrarecontabilidade.com.br/health`
2. `https://aluguel.lucrarecontabilidade.com.br/` — landing
3. Formulário **Ver Demonstração** → e-mail em `tthimoteoconsultoria@gmail.com`
4. Cadastro Trial / login → `/inicio`

---

## 3. Variáveis de ambiente (referência completa)

### API (Render)

| Chave | Obrigatória | Notas |
|---|---|---|
| `ConnectionStrings__Postgres` | sim | Preenchida pelo Blueprint (`fromDatabase`) |
| `Jwt__SigningKey` | sim | ≥ 32 chars; **nunca** reutilize a de desenvolvimento |
| `Cors__Origins__0` | sim | URL HTTPS do frontend |
| `Email__GraphTenantId` / `ClientId` / `ClientSecret` | para demo | Mesmos valores do Graph já validados |
| `InterPix__Ambiente` | MVP = `Mock` | Trocar para `Producao` + cert. Inter depois |
| `Nfse__Ambiente` | MVP = `Mock` | |
| `Boleto__Ambiente` | MVP = `Mock` | |

A API normaliza automaticamente URI `postgres://` do Blueprint Render para o formato Npgsql (`Host=...`) com `SSL Mode=Require`. Se ainda falhar, edite `ConnectionStrings__Postgres` manualmente no painel (formato chave=valor).

### Frontend (Vercel)

| Chave | Obrigatória | Notas |
|---|---|---|
| `API_URL` | sim | URL pública HTTPS da API (sem barra final) |

O browser só chama rotas `/api/*` do Next; o Next faz proxy para a API. Por isso `API_URL` **não** precisa ser `NEXT_PUBLIC_*`.

---

## 4. Alternativa: frontend também no Render

Se preferir **não** criar conta Vercel:

1. No Render: **New → Web Service** → mesmo repo, **Root Directory** `web`.
2. Runtime **Node**, Build: `npm install && npm run build`, Start: `npm run start`.
3. Env: `API_URL=https://api.aluguel...`, `PORT` (Render injeta).
4. Custom domain `aluguel.lucrarecontabilidade.com.br` no serviço Node.

A arquitetura oficial recomenda Vercel (CDN + edge); Render Node funciona para MVP.

---

## 5. Supabase (opcional, depois do MVP)

Para Storage (contratos, certificados, DANFSe) e Postgres gerenciado:

1. Crie projeto em [supabase.com](https://supabase.com).
2. Copie a connection string (modo Session / Transaction) para `ConnectionStrings__Postgres`.
3. Configure buckets conforme `07-Estrategia-Supabase-Storage.md`.

No MVP atual o storage local em disco **não** persiste bem em container efêmero — anexos/certificados em produção pedem Supabase Storage ou disco persistente.

---

## 6. Segurança pós-deploy

- Rotacione `Jwt__SigningKey` se vazou em log/chat.
- Não committe secrets; use só o dashboard Render/Vercel.
- Mantenha `Email__GraphClientSecret` só no Render.
- Quando for ao ar com PIX real: certificado Inter + webhook HTTPS público apontando para a API.

---

## 7. Troubleshooting rápido

| Sintoma | Causa comum | Ação |
|---|---|---|
| API 502 / sleep | Plano Free dormindo | Aguarde wake ou upgrade Starter |
| Frontend login falha | `API_URL` errada ou CORS | Confira env Vercel + `Cors__Origins__*` |
| Demo sem e-mail | Graph sem `Mail.Send` / secret | Revisar doc `16-Email-Microsoft-Graph.md` |
| Migração falha | SSL Postgres | Ajuste connection string (SSL Mode) |
| Build Docker falha | branch sem `Dockerfile` | Faça merge do PR de deploy em `main` |
