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

# Next.js às vezes devolve HTML incompleto em GETs seguidos; tenta de novo até achar um padrão.
# Grep no arquivo (não via echo) para não corromper HTML com NUL / escapes.
fetch_html_matching() {
  local cookie="$1" url="$2" modo="$3"
  shift 3
  local i
  for i in 1 2 3 4 5; do
    CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b "$cookie" "$url") || CODE=000
    BODY=$(cat /tmp/fe_body 2>/dev/null || true)
    if [ "$CODE" = "200" ]; then
      local pat ok
      if [ "$modo" = "all" ]; then
        ok=1
        for pat in "$@"; do
          if ! grep -aFq "$pat" /tmp/fe_body; then ok=0; break; fi
        done
        [ "$ok" = "1" ] && return 0
      else
        for pat in "$@"; do
          if grep -aFq "$pat" /tmp/fe_body; then
            return 0
          fi
        done
      fi
    fi
    sleep 0.4
  done
  return 1
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
if fetch_html_matching /tmp/fe_cookies "$WEB/usuarios" any \
  "Selecione um cliente para gerenciar" 'href="/usuarios?clienteId='; then
  check 200 "$CODE" "GET /usuarios (gestor lista clientes)"
  echo "PASS [usuarios gestor seletor de clientes]"
  PASS=$((PASS+1))
else
  check 200 "$CODE" "GET /usuarios (gestor lista clientes)"
  echo "FAIL [usuarios gestor seletor de clientes] body sem seletor"
  FAIL=$((FAIL+1))
fi
if fetch_html_matching /tmp/fe_cookies "$WEB/imoveis" any \
  "Selecione um cliente para ver os imóveis" "Cadastro de imóveis" 'href="/imoveis?clienteId='; then
  check 200 "$CODE" "GET /imoveis (gestor)"
  echo "PASS [imoveis gestor seletor ou lista]"
  PASS=$((PASS+1))
else
  check 200 "$CODE" "GET /imoveis (gestor)"
  echo "FAIL [imoveis gestor seletor ou lista] body inesperado"
  FAIL=$((FAIL+1))
fi

line "FORMULÁRIO INCLUIR IMÓVEL"
if fetch_html_matching /tmp/fe_cookies "$WEB/imoveis/novo" all \
  'name="clienteId"' 'name="nome"' 'name="tipo"' 'name="cep"' 'name="logradouro"'; then
  check 200 "$CODE" "GET /imoveis/novo (gestor)"
  echo "PASS [formulario imovel campos]"
  PASS=$((PASS+1))
else
  check 200 "$CODE" "GET /imoveis/novo (gestor)"
  echo "FAIL [formulario imovel campos] faltam campos do cadastro len=${#BODY}"
  FAIL=$((FAIL+1))
fi
CEP_POS=$(python3 - <<'PY'
h=open("/tmp/fe_body","rb").read().decode("utf-8","replace")
print(h.find('name="cep"'))
PY
)
LOG_POS=$(python3 - <<'PY'
h=open("/tmp/fe_body","rb").read().decode("utf-8","replace")
print(h.find('name="logradouro"'))
PY
)
if [ "$CEP_POS" -ge 0 ] && [ "$LOG_POS" -gt "$CEP_POS" ]; then
  echo "PASS [formulario imovel CEP antes do logradouro]"
  PASS=$((PASS+1))
else
  echo "FAIL [formulario imovel CEP antes do logradouro] cep=$CEP_POS logradouro=$LOG_POS"
  FAIL=$((FAIL+1))
fi
if grep -aEq 'name="cep"[^>]*required' /tmp/fe_body \
  && grep -aEq 'name="logradouro"[^>]*required' /tmp/fe_body \
  && grep -aEq 'name="numero"[^>]*required' /tmp/fe_body \
  && grep -aEq 'name="bairro"[^>]*required' /tmp/fe_body \
  && grep -aEq 'name="cidade"[^>]*required' /tmp/fe_body \
  && grep -aEq 'name="uf"[^>]*required' /tmp/fe_body; then
  echo "PASS [formulario imovel endereco obrigatorio]"
  PASS=$((PASS+1))
else
  echo "FAIL [formulario imovel endereco obrigatorio] CEP/logradouro/número/bairro/cidade/UF sem required"
  FAIL=$((FAIL+1))
fi
if grep -aEq 'data-cliente-inicial="[0-9a-fA-F-]{36}"' /tmp/fe_body; then
  echo "PASS [formulario imovel cliente pre-selecionado]"
  PASS=$((PASS+1))
else
  echo "FAIL [formulario imovel cliente pre-selecionado] dropdown sem cliente inicial"
  FAIL=$((FAIL+1))
fi

line "CONSULTA CEP"
CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' "$WEB/api/cep/01001000")
BODY=$(cat /tmp/fe_body)
check 401 "$CODE" "GET /api/cep sem autenticacao"
CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies "$WEB/api/cep/01001000")
BODY=$(cat /tmp/fe_body)
check 200 "$CODE" "GET /api/cep/01001000 (gestor)"
if echo "$BODY" | grep -q "Praça da Sé" && echo "$BODY" | grep -q '"uf":"SP"'; then
  echo "PASS [consulta CEP preenche logradouro cidade UF]"
  PASS=$((PASS+1))
else
  echo "FAIL [consulta CEP preenche logradouro cidade UF] body=${BODY:0:240}"
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
    if fetch_html_matching /tmp/fe_cookies "$WEB/imoveis?clienteId=$CLIENTE_GESTOR" all \
      "$NOME_E2E"; then
      check 200 "$CODE" "GET /imoveis lista do cliente"
      if grep -aFq "href=\"/imoveis/$IMV_ID\"" /tmp/fe_body; then
        echo "PASS [lista imoveis link para cadastro]"
        PASS=$((PASS+1))
      else
        echo "FAIL [lista imoveis link para cadastro] sem href do imóvel criado"
        FAIL=$((FAIL+1))
      fi
      if grep -aFq 'href="/inquilinos' /tmp/fe_body || grep -aFq 'href="/contratos' /tmp/fe_body; then
        echo "FAIL [lista imoveis sem inquilinos/contratos] ainda tem atalhos na listagem"
        FAIL=$((FAIL+1))
      else
        echo "PASS [lista imoveis sem inquilinos/contratos]"
        PASS=$((PASS+1))
      fi
    else
      check 200 "$CODE" "GET /imoveis lista do cliente"
      echo "FAIL [lista imoveis link para cadastro] lista sem o imóvel criado"
      FAIL=$((FAIL+2))
    fi
    if [ -n "$IMV_ID" ] && fetch_html_matching /tmp/fe_cookies "$WEB/imoveis/$IMV_ID" all \
      "$NOME_E2E" "Inquilino" "Contrato"; then
      check 200 "$CODE" "GET /imoveis/{id} cadastro"
      echo "PASS [cadastro imovel inquilino e contrato]"
      PASS=$((PASS+1))
      if grep -aFq 'name="nome"' /tmp/fe_body && grep -aFq 'name="status"' /tmp/fe_body \
        && grep -aEq 'name="cep"[^>]*required' /tmp/fe_body; then
        echo "PASS [cadastro imovel formulario editavel gestor]"
        PASS=$((PASS+1))
      else
        echo "FAIL [cadastro imovel formulario editavel gestor] sem campos editáveis"
        FAIL=$((FAIL+1))
      fi
    else
      check 200 "$CODE" "GET /imoveis/{id} cadastro"
      echo "FAIL [cadastro imovel inquilino e contrato] body sem seções len=${#BODY}"
      FAIL=$((FAIL+1))
    fi
    NOME_EDIT="${NOME_E2E} editado"
    CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies -X PUT "$WEB/api/imoveis/$IMV_ID" \
      -H 'Content-Type: application/json' \
      -d "{\"nome\":\"$NOME_EDIT\",\"tipo\":\"Residencial\",\"numeroIptu\":\"IPTU-E2E\",\"status\":\"Ativo\",\"endereco\":{\"logradouro\":\"Praca da Se\",\"numero\":\"100\",\"bairro\":\"Se\",\"cidade\":\"Sao Paulo\",\"uf\":\"SP\",\"cep\":\"01001000\"}}")
    BODY=$(cat /tmp/fe_body)
    check 200 "$CODE" "PUT /api/imoveis/{id} via BFF"
    if echo "$BODY" | grep -q "$NOME_EDIT"; then
      echo "PASS [PUT imovel nome]"
      PASS=$((PASS+1))
    else
      echo "FAIL [PUT imovel nome] body=${BODY:0:240}"
      FAIL=$((FAIL+1))
    fi
    TOKEN=$(curl -s -X POST "$API/api/auth/login" -H 'Content-Type: application/json' \
      -d '{"email":"gestor@demo.local","senha":"Gestor@123456"}' | jq -r '.accessToken // empty')
    if [ -n "$IMV_ID" ] && [ -n "$TOKEN" ] && [ "$TOKEN" != "null" ]; then
      curl -s -o /dev/null -X DELETE "$API/api/imoveis/$IMV_ID" -H "Authorization: Bearer $TOKEN"
    fi
  elif echo "$BODY" | grep -q "Limite de imóveis"; then
    echo "PASS [POST /api/imoveis limite do plano] http=$CODE"
    PASS=$((PASS+8))
  else
    echo "FAIL [POST /api/imoveis] esperado=201 obtido=$CODE body=${BODY:0:240}"
    FAIL=$((FAIL+8))
  fi
else
  echo "FAIL [POST imovel sem clienteId do JWT]"
  FAIL=$((FAIL+8))
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
