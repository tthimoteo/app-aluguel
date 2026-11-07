# E-mail via Microsoft Graph (OAuth2)

Envio transacional do APP Aluguel (ex.: lead de demonstração do plano Pro) usando **Microsoft Graph** com **OAuth2 client credentials** — sem senha de app SMTP.

## Fluxo

1. App registra no Entra ID (Azure AD) do tenant Lucrare.
2. Backend obtém token `client_credentials` (`https://graph.microsoft.com/.default`).
3. Chama `POST /users/{remetente}/sendMail` enviando para `tthimoteoconsultoria@gmail.com`.
4. `Reply-To` = e-mail preenchido no formulário público.

## Registro no Entra ID (admin)

1. **Entra ID** → **Registros de aplicativo** → **Novo registro**  
   - Nome: `APP Aluguel · E-mail`  
   - Contas: somente este diretório organizacional.
2. **Certificados e segredos** → **Novo segredo do cliente** → copiar o valor (só aparece uma vez).
3. **Permissões de API** → **Microsoft Graph** → **Permissões de aplicativo**:
   - `Mail.Send`
4. **Conceder consentimento de administrador** para o tenant.
5. Anotar:
   - **ID do diretório (tenant)**
   - **ID do aplicativo (client)**
   - **Segredo do cliente**
6. A caixa remetente (ex.: `thiago.thimoteo@lucrarecontabilidade.com.br`) precisa existir no Exchange Online; o app envia **como** essa caixa.

> Observação: com permissão de aplicativo `Mail.Send`, o app pode enviar em nome de qualquer caixa do tenant. Em produção avançada, restrinja com **Application Access Policy** do Exchange (`New-ApplicationAccessPolicy`) à caixa desejada.

## Secrets / configuração

| Variável | Uso |
|---|---|
| `EMAIL_GRAPH_TENANT_ID` | ID do tenant Entra ID |
| `EMAIL_GRAPH_CLIENT_ID` | ID do aplicativo |
| `EMAIL_GRAPH_CLIENT_SECRET` | Segredo do cliente |
| `EMAIL_GRAPH_SENDER` | UPN da caixa remetente (opcional; padrão no appsettings) |

`Email:Ambiente` = `Graph` (ou deixe automático: se as 3 credenciais Graph existirem, o DI escolhe Graph).

SMTP básico (`EMAIL_SMTP_*`) permanece como fallback legado; O365 sem senha de app deve preferir Graph.

## Endpoint que dispara o envio

`POST /api/cadastro-publico/demonstracao` → `SolicitarDemonstracaoCommand` → `IEmailSender` (implementação Graph).
