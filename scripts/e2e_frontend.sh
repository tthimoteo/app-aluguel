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

line "LOGIN BFF (Gestor)"
CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -c /tmp/fe_cookies -X POST "$WEB/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d '{"email":"gestor@demo.local","senha":"Gestor@123456"}')
BODY=$(cat /tmp/fe_body)
check 200 "$CODE" "login gestor via Next.js"
echo "  nome=$(echo "$BODY" | jq -r '.usuario.nome // empty')"

line "PÁGINAS AUTENTICADAS"
for path in / /imoveis /inquilinos /contratos /minha-conta /usuarios; do
  CODE=$(curl -s -o /tmp/fe_body -w '%{http_code}' -b /tmp/fe_cookies "$WEB$path")
  BODY=$(cat /tmp/fe_body)
  check 200 "$CODE" "GET $path"
done

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
