#!/usr/bin/env bash
# CASO 1 / CASO 1b — CPF único no cliente; mesmo CPF em clientes distintos com perfil por vinculação.
set -uo pipefail
BASE="${ALUGUEL_API_URL:-http://127.0.0.1:5272}"
PASS=0; FAIL=0
CODE=""; BODY=""
line() { printf '\n=== %s ===\n' "$1"; }
check() {
  if [ "$1" = "$2" ]; then echo "PASS [$3] http=$2"; PASS=$((PASS+1));
  else echo "FAIL [$3] esperado=$1 obtido=$2 body=${BODY:0:400}"; FAIL=$((FAIL+1)); fi
}
login() {
  curl -s -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' \
    -d "{\"email\":\"$1\",\"senha\":\"$2\"}"
}
req() {
  local m=$1 p=$2 t=$3 d=${4:-}
  if [ -n "$d" ]; then
    CODE=$(curl -s -o /tmp/e2e_body -w '%{http_code}' -X "$m" "$BASE$p" \
      -H "Authorization: Bearer $t" -H 'Content-Type: application/json' -d "$d")
  else
    CODE=$(curl -s -o /tmp/e2e_body -w '%{http_code}' -X "$m" "$BASE$p" -H "Authorization: Bearer $t")
  fi
  BODY=$(cat /tmp/e2e_body)
}

cpf_novo() {
  python3 - <<'PY'
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
}

line "LOGIN admin"
ADMIN_JSON=$(login admin@aluguel.local Admin@123456)
ADMIN=$(echo "$ADMIN_JSON" | jq -r '.accessToken')
[ -n "$ADMIN" ] && [ "$ADMIN" != "null" ] && echo "admin OK" || { echo "admin FALHOU"; exit 1; }

line "Clientes Demo e Extra"
req GET "/api/clientes?take=20" "$ADMIN"
check 200 "$CODE" "listar clientes"
CLIENTE_A=$(echo "$BODY" | jq -r '.itens[] | select(.cpf=="39053344705") | .id' | head -1)
CLIENTE_B=$(echo "$BODY" | jq -r '.itens[] | select(.cpf=="11144477735") | .id' | head -1)
echo "  A=$CLIENTE_A B=$CLIENTE_B"
if [ -z "$CLIENTE_A" ] || [ -z "$CLIENTE_B" ] || [ "$CLIENTE_A" = "null" ] || [ "$CLIENTE_B" = "null" ]; then
  echo "FAIL [clientes demo/extra ausentes]"
  FAIL=$((FAIL+1))
  echo "TOTAL pass=$PASS fail=$FAIL"
  exit 1
fi

CPF=$(cpf_novo)
EMAIL="e2e-multi-$(date +%s)@demo.local"
SENHA="Usuario@123456"

line "CASO 1b — cadastra no cliente A e vincula o mesmo CPF no B"
req POST /api/usuarios "$ADMIN" "{\"clienteId\":\"$CLIENTE_A\",\"nome\":\"Multi Cliente\",\"email\":\"$EMAIL\",\"cpf\":\"$CPF\",\"perfil\":\"Gestor\",\"senha\":\"$SENHA\"}"
check 201 "$CODE" "criar usuário no cliente A"
USER_ID=$(echo "$BODY" | jq -r '.id')
echo "  id=$USER_ID perfil=$(echo "$BODY" | jq -r '.perfil')"

req POST /api/usuarios "$ADMIN" "{\"clienteId\":\"$CLIENTE_A\",\"nome\":\"Multi Cliente\",\"email\":\"$EMAIL\",\"cpf\":\"$CPF\",\"perfil\":\"Analista\",\"senha\":\"$SENHA\"}"
check 400 "$CODE" "CASO 1: mesmo CPF no mesmo cliente -> 400"

req POST /api/usuarios "$ADMIN" "{\"clienteId\":\"$CLIENTE_B\",\"nome\":\"Multi Cliente\",\"email\":\"outro@demo.local\",\"cpf\":\"$CPF\",\"perfil\":\"Analista\"}"
check 201 "$CODE" "CASO 1b: mesmo CPF no outro cliente -> 201"
ID_B=$(echo "$BODY" | jq -r '.id')
PERFIL_B=$(echo "$BODY" | jq -r '.perfil')
if [ "$ID_B" = "$USER_ID" ]; then echo "PASS [mesma identidade]"; PASS=$((PASS+1)); else echo "FAIL [id diferente] $ID_B vs $USER_ID"; FAIL=$((FAIL+1)); fi
if [ "$PERFIL_B" = "Analista" ]; then echo "PASS [perfil Analista no cliente B]"; PASS=$((PASS+1)); else echo "FAIL [perfil B=$PERFIL_B]"; FAIL=$((FAIL+1)); fi

line "Login do usuário multi-cliente"
LOGIN_JSON=$(login "$EMAIL" "$SENHA")
TOKEN=$(echo "$LOGIN_JSON" | jq -r '.accessToken')
N_CLIENTES=$(echo "$LOGIN_JSON" | jq -r '.usuario.clientes | length')
ROLE=$(echo "$LOGIN_JSON" | jq -r '.usuario.roles[0]')
REFRESH=$(echo "$LOGIN_JSON" | jq -r '.refreshToken')
[ -n "$TOKEN" ] && [ "$TOKEN" != "null" ] && echo "PASS [login multi]"; PASS=$((PASS+1)) || { echo "FAIL [login multi]"; FAIL=$((FAIL+1)); }
if [ "$N_CLIENTES" = "2" ]; then echo "PASS [2 vinculações no login]"; PASS=$((PASS+1)); else echo "FAIL [clientes=$N_CLIENTES]"; FAIL=$((FAIL+1)); fi

line "Troca de contexto (perfil por cliente)"
OUTRO=$(echo "$LOGIN_JSON" | jq -r --arg cur "$(echo "$LOGIN_JSON" | jq -r '.usuario.clienteId')" '.usuario.clientes[] | select(.clienteId != $cur) | .clienteId' | head -1)
req POST /api/auth/contexto "$TOKEN" "{\"clienteId\":\"$OUTRO\",\"refreshToken\":\"$REFRESH\"}"
check 200 "$CODE" "POST /api/auth/contexto"
ROLE2=$(echo "$BODY" | jq -r '.usuario.roles[0]')
CID2=$(echo "$BODY" | jq -r '.usuario.clienteId')
echo "  role_inicial=$ROLE role_novo=$ROLE2 cliente=$CID2"
if [ "$ROLE" != "$ROLE2" ]; then echo "PASS [perfil muda conforme o cliente]"; PASS=$((PASS+1)); else echo "FAIL [perfil não mudou] $ROLE2"; FAIL=$((FAIL+1)); fi
if [ "$CID2" = "$OUTRO" ]; then echo "PASS [cliente_id do JWT atualizado]"; PASS=$((PASS+1)); else echo "FAIL [cliente_id=$CID2]"; FAIL=$((FAIL+1)); fi

echo
echo "TOTAL pass=$PASS fail=$FAIL"
[ "$FAIL" -eq 0 ]
