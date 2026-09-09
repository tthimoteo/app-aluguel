#!/usr/bin/env bash
# E2E do frontend: login BFF (Next.js) contra a API .NET e páginas autenticadas.
set -uo pipefail
API="${API_URL:-http://127.0.0.1:5272}"
WEB="${WEB_URL:-http://127.0.0.1:3000}"
PASS=0; FAIL=0
CODE=""; BODY=""
line() { printf '\n=== %s ===\n' "$1"; }
check() {
  if [ "$1" = "$2" ]; then echo "PASS [$3] http=$2"; PASS=$((PASS+1));
  else echo "FAIL [$3] esperado=$1 obtido=$2 body=${BODY:0:200}"; FAIL=$((FAIL+1)); fi
}

line "API /health"
CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' "$API/health") || CODE=000
BODY=$(cat /tmp/fe_body 2>/dev/null || true)
check 200 "$CODE" "API saudável"

line "LOGIN BFF (Administrador)"
CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -c /tmp/fe_cookies_admin -X POST "$WEB/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@aluguel.local","senha":"Admin@123456"}')
BODY=$(cat /tmp/fe_body)
check 200 "$CODE" "login admin via Next.js"

line "HOME ADMIN (lista de clientes)"
CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies_admin "$WEB/")
BODY=$(cat /tmp/fe_body)
check 200 "$CODE" "GET / (admin)"
if echo "$BODY" | grep -q "Nome (razão social)" && echo "$BODY" | grep -q "CPF ou CNPJ"; then
  echo "PASS [home admin lista clientes]"
  PASS=$((PASS+1))
else
  echo "FAIL [home admin lista clientes] body sem colunas esperadas"
  FAIL=$((FAIL+1))
fi
if echo "$BODY" | grep -q "ClienteID"; then
  echo "FAIL [home admin sem ClienteID] ainda renderiza ClienteID"
  FAIL=$((FAIL+1))
else
  echo "PASS [home admin sem ClienteID]"
  PASS=$((PASS+1))
fi
if echo "$BODY" | grep -q "Cadastro da plataforma"; then
  echo "PASS [home admin card clientes]"
  PASS=$((PASS+1))
else
  echo "FAIL [home admin card clientes] sem card de quantidade de clientes"
  FAIL=$((FAIL+1))
fi
if echo "$BODY" | grep -q "Pessoas físicas e jurídicas"; then
  echo "FAIL [home admin sem card inquilinos] ainda renderiza card de inquilinos"
  FAIL=$((FAIL+1))
else
  echo "PASS [home admin sem card inquilinos]"
  PASS=$((PASS+1))
fi
if echo "$BODY" | grep -q "Ativos, encerrados e cancelados"; then
  echo "FAIL [home admin sem card contratos] ainda renderiza card de contratos"
  FAIL=$((FAIL+1))
else
  echo "PASS [home admin sem card contratos]"
  PASS=$((PASS+1))
fi
if echo "$BODY" | grep -q ">Imóveis</h3>"; then
  echo "FAIL [home admin sem lista de imóveis] ainda renderiza h3 Imóveis"
  FAIL=$((FAIL+1))
else
  echo "PASS [home admin sem lista de imóveis]"
  PASS=$((PASS+1))
fi
if echo "$BODY" | grep -q "sm:grid-cols-3"; then
  echo "FAIL [home admin sem atalhos] ainda renderiza grade de atalhos"
  FAIL=$((FAIL+1))
else
  echo "PASS [home admin sem atalhos]"
  PASS=$((PASS+1))
fi
if echo "$BODY" | grep -q 'href="/relatorios"'; then
  echo "PASS [menu admin tem relatorios]"
  PASS=$((PASS+1))
else
  echo "FAIL [menu admin tem relatorios] Relatórios ausente do menu lateral"
  FAIL=$((FAIL+1))
fi
if echo "$BODY" | grep -q 'href="/inquilinos"' || echo "$BODY" | grep -q 'href="/contratos"'; then
  echo "FAIL [menu admin sem inquilinos/contratos] ainda renderiza itens no menu"
  FAIL=$((FAIL+1))
else
  echo "PASS [menu admin sem inquilinos/contratos]"
  PASS=$((PASS+1))
fi

CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies_admin "$WEB/relatorios")
BODY=$(cat /tmp/fe_body)
check 200 "$CODE" "GET /relatorios (admin)"

line "USUÁRIOS ADMIN (CRUD)"
CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies_admin "$WEB/usuarios")
BODY=$(cat /tmp/fe_body)
check 200 "$CODE" "GET /usuarios (admin lista clientes)"
python3 - <<'PY' >/tmp/fe_usuarios_crud.env
import json, os, re, urllib.error, urllib.request
from pathlib import Path

api = os.environ.get("API_URL", "http://127.0.0.1:5272")
token = ""
for line in Path("/tmp/fe_cookies_admin").read_text().splitlines():
    parts = line.split()
    if len(parts) >= 7 and parts[-2] == "aluguel_access":
        token = parts[-1]
        break
if not token:
    raise SystemExit("sem token")
ids = re.findall(r"clienteId=([0-9a-f-]{36})", Path("/tmp/fe_body").read_text())
if not ids:
    raise SystemExit("sem clienteId")

def req(method, url, data=None):
    body = None if data is None else json.dumps(data).encode()
    headers = {"Authorization": f"Bearer {token}", "Accept": "application/json"}
    if body is not None:
        headers["Content-Type"] = "application/json"
    r = urllib.request.Request(url, data=body, method=method, headers=headers)
    try:
        with urllib.request.urlopen(r) as res:
            raw = res.read()
            return res.status, raw.decode() if raw else ""
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode()

cliente_id = None
for cid in dict.fromkeys(ids):
    st, raw = req("GET", f"{api}/api/clientes/{cid}")
    if st != 200:
        continue
    if not json.loads(raw).get("planoId"):
        continue
    st, raw = req("GET", f"{api}/api/usuarios?clienteId={cid}&take=50")
    if st != 200:
        continue
    itens = json.loads(raw).get("itens", [])
    for u in itens:
        email = str(u.get("email") or "")
        if email.startswith("e2e-") and u.get("status") == "Ativo":
            req("DELETE", f"{api}/api/usuarios/{u['id']}")
    cliente_id = cid
    break

if not cliente_id:
    cliente_id = ids[0]
print(f"CLIENTE_ID={cliente_id}")
PY
# shellcheck disable=SC1091
. /tmp/fe_usuarios_crud.env
echo "  clienteId=$CLIENTE_ID"
CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies_admin "$WEB/usuarios?clienteId=$CLIENTE_ID")
BODY=$(cat /tmp/fe_body)
check 200 "$CODE" "GET /usuarios?clienteId (admin)"
if echo "$BODY" | grep -q "Incluir usuário"; then
  echo "PASS [admin incluir usuário visível]"
  PASS=$((PASS+1))
else
  echo "FAIL [admin incluir usuário visível]"
  FAIL=$((FAIL+1))
fi
EMAIL_E2E="e2e-admin-$(date +%s)@demo.local"
CPF_E2E=$(python3 - <<'PY'
import random
def dv(nums, q, p0):
    s = sum(nums[i] * (p0 - i) for i in range(q))
    r = s % 11
    return 0 if r < 2 else 11 - r
while True:
    n = [random.randint(0, 9) for _ in range(9)]
    n.append(dv(n, 9, 10)); n.append(dv(n, 10, 11))
    s = "".join(map(str, n))
    if len(set(s)) > 1 and s not in {"39053344705", "52998224725", "11144477735"}:
        print(s); break
PY
)
CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies_admin -X POST "$WEB/api/usuarios" \
  -H 'Content-Type: application/json' \
  -d "{\"clienteId\":\"$CLIENTE_ID\",\"nome\":\"E2E Admin User\",\"email\":\"$EMAIL_E2E\",\"cpf\":\"$CPF_E2E\",\"perfil\":\"Analista\",\"senha\":\"Usuario@123456\"}")
BODY=$(cat /tmp/fe_body)
check 201 "$CODE" "POST /api/usuarios (admin)"
USER_ID=$(echo "$BODY" | jq -r '.id // empty')
if [ -n "$USER_ID" ] && [ "$USER_ID" != "null" ]; then
  CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies_admin "$WEB/api/usuarios/$USER_ID?clienteId=$CLIENTE_ID")
  check 200 "$CODE" "GET /api/usuarios/{id} (admin)"
  CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies_admin -X PUT "$WEB/api/usuarios/$USER_ID?clienteId=$CLIENTE_ID" \
    -H 'Content-Type: application/json' \
    -d "{\"nome\":\"E2E Admin Editado\",\"email\":\"e2e-admin-edit-$(date +%s)@demo.local\",\"telefone\":\"11988887777\",\"perfil\":\"Analista\",\"status\":\"Ativo\"}")
  check 200 "$CODE" "PUT /api/usuarios/{id} (admin)"
  EMAIL_EDITADO=$(echo "$(cat /tmp/fe_body)" | jq -r '.email // empty')
  if echo "$EMAIL_EDITADO" | grep -q 'e2e-admin-edit-'; then
    echo "PASS [PUT altera e-mail]"
    PASS=$((PASS+1))
  else
    echo "FAIL [PUT altera e-mail] email=$EMAIL_EDITADO"
    FAIL=$((FAIL+1))
  fi
  CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies_admin -X DELETE "$WEB/api/usuarios/$USER_ID?clienteId=$CLIENTE_ID")
  check 204 "$CODE" "DELETE /api/usuarios/{id} (admin)"
else
  echo "FAIL [criar usuário sem id] body=${BODY:0:300}"
  FAIL=$((FAIL+3))
fi

line "LOGIN BFF (Gestor)"
CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -c /tmp/fe_cookies -X POST "$WEB/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d '{"email":"gestor@demo.local","senha":"Gestor@123456"}')
BODY=$(cat /tmp/fe_body)
check 200 "$CODE" "login gestor via Next.js"
echo "  nome=$(echo "$BODY" | jq -r '.usuario.nome // empty')"
CLIENTE_GESTOR=$(echo "$BODY" | jq -r '.usuario.clienteId // empty')

line "PÁGINAS AUTENTICADAS"
for path in / /imoveis /imoveis/novo /inquilinos /contratos /minha-conta /usuarios /relatorios; do
  CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies "$WEB$path")
  BODY=$(cat /tmp/fe_body)
  check 200 "$CODE" "GET $path"
done

line "HOME GESTOR (lista de imóveis)"
CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies "$WEB/")
BODY=$(cat /tmp/fe_body)
check 200 "$CODE" "GET / (gestor)"
if echo "$BODY" | grep -q ">Imóveis</h3>"; then
  echo "PASS [home gestor lista imóveis]"
  PASS=$((PASS+1))
else
  echo "FAIL [home gestor lista imóveis] body sem h3 Imóveis"
  FAIL=$((FAIL+1))
fi
if echo "$BODY" | grep -q "Nome (razão social)"; then
  echo "FAIL [home gestor sem lista de clientes] renderiza colunas de clientes"
  FAIL=$((FAIL+1))
else
  echo "PASS [home gestor sem lista de clientes]"
  PASS=$((PASS+1))
fi
if echo "$BODY" | grep -q "Pessoas físicas e jurídicas" && echo "$BODY" | grep -q "Ativos, encerrados e cancelados"; then
  echo "PASS [home gestor cards inquilinos e contratos]"
  PASS=$((PASS+1))
else
  echo "FAIL [home gestor cards inquilinos e contratos] cards operacionais ausentes"
  FAIL=$((FAIL+1))
fi
if echo "$BODY" | grep -q "Cadastro da plataforma"; then
  echo "FAIL [home gestor sem card clientes] renderiza card de clientes"
  FAIL=$((FAIL+1))
else
  echo "PASS [home gestor sem card clientes]"
  PASS=$((PASS+1))
fi
if echo "$BODY" | grep -q "sm:grid-cols-3"; then
  echo "FAIL [home gestor sem atalhos] ainda renderiza grade de atalhos"
  FAIL=$((FAIL+1))
else
  echo "PASS [home gestor sem atalhos]"
  PASS=$((PASS+1))
fi
if echo "$BODY" | grep -q 'href="/relatorios"'; then
  echo "PASS [menu gestor tem relatorios]"
  PASS=$((PASS+1))
else
  echo "FAIL [menu gestor tem relatorios] Relatórios ausente do menu lateral"
  FAIL=$((FAIL+1))
fi
if echo "$BODY" | grep -q 'href="/imoveis/novo"'; then
  echo "PASS [home gestor incluir imovel]"
  PASS=$((PASS+1))
else
  echo "FAIL [home gestor incluir imovel] botão Incluir imóvel não aponta para o formulário"
  FAIL=$((FAIL+1))
fi
if echo "$BODY" | grep -q 'href="/inquilinos"' || echo "$BODY" | grep -q 'href="/contratos"'; then
  echo "FAIL [menu gestor sem inquilinos/contratos] ainda renderiza itens no menu"
  FAIL=$((FAIL+1))
else
  echo "PASS [menu gestor sem inquilinos/contratos]"
  PASS=$((PASS+1))
fi

line "USUÁRIOS E IMÓVEIS (GESTOR)"
CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies "$WEB/usuarios")
BODY=$(cat /tmp/fe_body)
check 200 "$CODE" "GET /usuarios (gestor lista clientes)"
if echo "$BODY" | grep -Fq "Selecione um cliente para gerenciar" || echo "$BODY" | grep -Fq 'href="/usuarios?clienteId='; then
  echo "PASS [usuarios gestor seletor de clientes]"
  PASS=$((PASS+1))
else
  echo "FAIL [usuarios gestor seletor de clientes] body sem seletor"
  FAIL=$((FAIL+1))
fi
CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies "$WEB/imoveis")
BODY=$(cat /tmp/fe_body)
check 200 "$CODE" "GET /imoveis (gestor)"
if echo "$BODY" | grep -Fq "Selecione um cliente para ver os imóveis" || echo "$BODY" | grep -Fq "Cadastro de imóveis" || echo "$BODY" | grep -Fq 'href="/imoveis?clienteId='; then
  echo "PASS [imoveis gestor seletor ou lista]"
  PASS=$((PASS+1))
else
  echo "FAIL [imoveis gestor seletor ou lista] body inesperado"
  FAIL=$((FAIL+1))
fi

line "FORMULÁRIO INCLUIR IMÓVEL"
CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies "$WEB/imoveis/novo")
BODY=$(cat /tmp/fe_body)
check 200 "$CODE" "GET /imoveis/novo (gestor)"
if echo "$BODY" | grep -q 'name="clienteId"' && echo "$BODY" | grep -q 'name="nome"' && echo "$BODY" | grep -q 'name="tipo"' && echo "$BODY" | grep -q 'name="logradouro"'; then
  echo "PASS [formulario imovel campos]"
  PASS=$((PASS+1))
else
  echo "FAIL [formulario imovel campos] faltam campos do cadastro"
  FAIL=$((FAIL+1))
fi
if [ -n "$CLIENTE_GESTOR" ] && echo "$BODY" | grep -q "$CLIENTE_GESTOR"; then
  echo "PASS [formulario imovel cliente pre-selecionado]"
  PASS=$((PASS+1))
else
  echo "FAIL [formulario imovel cliente pre-selecionado] clienteId do JWT ausente no dropdown"
  FAIL=$((FAIL+1))
fi
NOME_E2E="E2E Imovel $(date +%s)"
if [ -n "$CLIENTE_GESTOR" ]; then
  CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies -X POST "$WEB/api/imoveis" \
    -H 'Content-Type: application/json' \
    -d "{\"clienteId\":\"$CLIENTE_GESTOR\",\"nome\":\"$NOME_E2E\",\"tipo\":\"Residencial\",\"numeroIptu\":\"IPTU-E2E\",\"endereco\":{\"cidade\":\"Sao Paulo\",\"uf\":\"SP\",\"cep\":\"01001000\"}}")
  BODY=$(cat /tmp/fe_body)
  if [ "$CODE" = "201" ]; then
    echo "PASS [POST /api/imoveis via BFF] http=201"
    PASS=$((PASS+1))
    NOME_CRIADO=$(echo "$BODY" | jq -r '.nome // empty')
    if [ "$NOME_CRIADO" = "$NOME_E2E" ]; then
      echo "PASS [POST imovel nome]"
      PASS=$((PASS+1))
    else
      echo "FAIL [POST imovel nome] nome=$NOME_CRIADO"
      FAIL=$((FAIL+1))
    fi
    IMV_ID=$(echo "$BODY" | jq -r '.id // empty')
    TOKEN=$(curl -s -X POST "$API/api/auth/login" -H 'Content-Type: application/json' \
      -d '{"email":"gestor@demo.local","senha":"Gestor@123456"}' | jq -r '.accessToken // empty')
    if [ -n "$IMV_ID" ] && [ -n "$TOKEN" ] && [ "$TOKEN" != "null" ]; then
      curl -s -o /dev/null -X DELETE "$API/api/imoveis/$IMV_ID" -H "Authorization: Bearer $TOKEN"
    fi
  elif echo "$BODY" | grep -q "Limite de imóveis"; then
    echo "PASS [POST /api/imoveis limite do plano] http=$CODE"
    PASS=$((PASS+2))
  else
    echo "FAIL [POST /api/imoveis] esperado=201 obtido=$CODE body=${BODY:0:240}"
    FAIL=$((FAIL+2))
  fi
else
  echo "FAIL [POST imovel sem clienteId do JWT]"
  FAIL=$((FAIL+2))
fi

line "LOGIN INVÁLIDO"
CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -X POST "$WEB/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d '{"email":"x@y.com","senha":"errada"}')
BODY=$(cat /tmp/fe_body)
check 401 "$CODE" "credencial inválida"

line "LOGOUT"
CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies -c /tmp/fe_cookies -X POST "$WEB/api/auth/logout")
BODY=$(cat /tmp/fe_body)
check 204 "$CODE" "logout"

echo
echo "resultado: PASS=$PASS FAIL=$FAIL"
[ "$FAIL" -eq 0 ]
