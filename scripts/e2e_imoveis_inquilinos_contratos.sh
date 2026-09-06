#!/usr/bin/env bash
set -uo pipefail
BASE=http://localhost:5272
PASS=0; FAIL=0
CODE=""; BODY=""
line() { printf '\n=== %s ===\n' "$1"; }
# check "<expected_http>" "<actual_http>" "<descricao>"
check() {
  if [ "$1" = "$2" ]; then echo "PASS [$3] http=$2"; PASS=$((PASS+1));
  else echo "FAIL [$3] esperado=$1 obtido=$2 body=$BODY"; FAIL=$((FAIL+1)); fi
}
login() {
  curl -s -X POST "$BASE/api/auth/login" -H 'Content-Type: application/json' \
    -d "{\"email\":\"$1\",\"senha\":\"$2\"}" | jq -r '.accessToken'
}
# req METHOD PATH TOKEN [JSON] -> define $CODE (http) e $BODY (corpo) no shell atual
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

line "LOGIN (Gestor / Analista / Admin)"
GESTOR=$(login gestor@demo.local Gestor@123456)
ANALISTA=$(login analista@demo.local Analista@123456)
ADMIN=$(login admin@aluguel.local Admin@123456)
[ -n "$GESTOR" ] && [ "$GESTOR" != null ] && echo "gestor OK" || echo "gestor FALHOU"
[ -n "$ANALISTA" ] && [ "$ANALISTA" != null ] && echo "analista OK" || echo "analista FALHOU"
[ -n "$ADMIN" ] && [ "$ADMIN" != null ] && echo "admin OK" || echo "admin FALHOU"

line "INQUILINOS - criar PF (CPF válido) e PJ (CNPJ válido)"
req POST /api/inquilinos "$GESTOR" '{"tipoPessoa":"PF","nome":"João da Silva","documento":"529.982.247-25","email":"joao@demo.local"}'
check 201 "$CODE" "criar inquilino PF"; INQ_PF=$(echo "$BODY" | jq -r '.id'); echo "  doc normalizado=$(echo "$BODY" | jq -r '.documento')"
req POST /api/inquilinos "$GESTOR" '{"tipoPessoa":"PJ","nome":"Imobiliaria XPTO","documento":"11.222.333/0001-81"}'
check 201 "$CODE" "criar inquilino PJ"; INQ_PJ=$(echo "$BODY" | jq -r '.id')
req POST /api/inquilinos "$GESTOR" '{"tipoPessoa":"PF","nome":"CPF Invalido","documento":"111.111.111-11"}'
check 400 "$CODE" "inquilino PF com CPF inválido -> 400"

line "IMOVEIS - CRUD + limite de plano (Intermediário: max_imoveis=5)"
IDS=()
for n in 1 2 3 4 5; do
  req POST /api/imoveis "$GESTOR" "{\"nome\":\"Imovel $n\",\"tipo\":\"Residencial\",\"endereco\":{\"cidade\":\"Sao Paulo\",\"uf\":\"SP\",\"cep\":\"01001-000\"}}"
  check 201 "$CODE" "criar imovel $n"; IDS+=("$(echo "$BODY" | jq -r '.id')")
done
req POST /api/imoveis "$GESTOR" '{"nome":"Imovel 6 (excede)","tipo":"Comercial"}'
check 400 "$CODE" "6o imovel excede limite do plano -> 400"; echo "  msg=$(echo "$BODY" | jq -r '.errors.Plano[0] // .detail')"
req GET /api/imoveis "$GESTOR"
check 200 "$CODE" "listar imoveis"; echo "  total=$(echo "$BODY" | jq -r '.total')"
IMV1=${IDS[0]}
req GET "/api/imoveis/$IMV1" "$GESTOR"; check 200 "$CODE" "obter imovel por id"
req PUT "/api/imoveis/$IMV1" "$GESTOR" '{"nome":"Imovel 1 (editado)","tipo":"Sala","numeroIptu":"IPTU-123","status":"Ativo"}'
check 200 "$CODE" "atualizar imovel"; echo "  nome=$(echo "$BODY" | jq -r '.nome') tipo=$(echo "$BODY" | jq -r '.tipo')"

line "IMOVEIS - inativar libera vaga do plano; reativar reavalia"
req PUT "/api/imoveis/$IMV1" "$GESTOR" '{"nome":"Imovel 1 (editado)","tipo":"Sala","status":"Inativo"}'
check 200 "$CODE" "inativar imovel 1 (agora 4 ativos)"
req POST /api/imoveis "$GESTOR" '{"nome":"Imovel 6 (agora cabe)","tipo":"Galpao"}'
check 201 "$CODE" "criar imovel apos inativar -> 201"; IMV6=$(echo "$BODY" | jq -r '.id')

line "CONTRATOS - criar (UC004) + CASO 3 (1 ativo por imovel)"
IMV2=${IDS[1]}
req POST /api/contratos "$GESTOR" "{\"imovelId\":\"$IMV2\",\"inquilinoId\":\"$INQ_PF\",\"numeroContrato\":\"CT-2026-001\",\"dataInicio\":\"2026-01-01\",\"dataFimPrevista\":\"2026-12-31\",\"diaVencimento\":10,\"valorAluguel\":2500.00}"
check 201 "$CODE" "criar contrato (Ativo)"; CT1=$(echo "$BODY" | jq -r '.id'); echo "  status=$(echo "$BODY" | jq -r '.status') clienteId=$(echo "$BODY" | jq -r '.clienteId')"
req POST /api/contratos "$GESTOR" "{\"imovelId\":\"$IMV2\",\"inquilinoId\":\"$INQ_PJ\",\"numeroContrato\":\"CT-2026-002\",\"dataInicio\":\"2026-02-01\",\"diaVencimento\":5,\"valorAluguel\":3000.00}"
check 400 "$CODE" "CASO 3: 2o contrato ativo no mesmo imovel -> 400"; echo "  msg=$(echo "$BODY" | jq -r '.detail')"
req POST /api/contratos "$GESTOR" "{\"imovelId\":\"$IMV2\",\"inquilinoId\":\"$INQ_PF\",\"numeroContrato\":\"CT-x\",\"dataInicio\":\"2026-01-01\",\"diaVencimento\":40,\"valorAluguel\":100}"
check 400 "$CODE" "dia_vencimento inválido -> 400"

line "DEPENDENTES (§4) - não remove imovel/inquilino com contrato ativo"
req DELETE "/api/imoveis/$IMV2" "$GESTOR"; check 400 "$CODE" "remover imovel com contrato ativo -> 400"; echo "  msg=$(echo "$BODY" | jq -r '.detail')"
req DELETE "/api/inquilinos/$INQ_PF" "$GESTOR"; check 400 "$CODE" "remover inquilino com contrato ativo -> 400"

line "CONTRATOS - encerrar libera o imovel; depois novo contrato; cancelar"
req POST "/api/contratos/$CT1/encerrar" "$GESTOR"; check 200 "$CODE" "encerrar contrato"; echo "  status=$(echo "$BODY" | jq -r '.status')"
req POST /api/contratos "$GESTOR" "{\"imovelId\":\"$IMV2\",\"inquilinoId\":\"$INQ_PJ\",\"numeroContrato\":\"CT-2026-003\",\"dataInicio\":\"2026-03-01\",\"diaVencimento\":5,\"valorAluguel\":3200.00}"
check 201 "$CODE" "novo contrato no mesmo imovel apos encerrar -> 201"; CT2=$(echo "$BODY" | jq -r '.id')
req POST "/api/contratos/$CT2/cancelar" "$GESTOR"; check 200 "$CODE" "cancelar contrato"; echo "  status=$(echo "$BODY" | jq -r '.status')"
req DELETE "/api/imoveis/$IMV2" "$GESTOR"; check 204 "$CODE" "remover imovel sem contrato ativo -> 204"

line "RBAC - Analista lê, mas não escreve"
req GET /api/imoveis "$ANALISTA"; check 200 "$CODE" "analista lista imoveis (200)"
req POST /api/imoveis "$ANALISTA" '{"nome":"Proibido","tipo":"Residencial"}'; check 403 "$CODE" "analista cria imovel -> 403"
req POST /api/contratos "$ANALISTA" "{\"imovelId\":\"$IMV6\",\"inquilinoId\":\"$INQ_PF\",\"numeroContrato\":\"CT-z\",\"dataInicio\":\"2026-01-01\",\"diaVencimento\":10,\"valorAluguel\":1000}"; check 403 "$CODE" "analista cria contrato -> 403"

line "REGRA - imovel e inquilino do mesmo cliente (via Administrador)"
req POST /api/clientes "$ADMIN" '{"tipoPessoa":"PJ","razaoSocial":"Outro Cliente LTDA","cnpj":"45.723.174/0001-10"}'
check 201 "$CODE" "admin cria 2o cliente"; CLI2=$(echo "$BODY" | jq -r '.id')
req POST /api/inquilinos "$ADMIN" "{\"clienteId\":\"$CLI2\",\"tipoPessoa\":\"PF\",\"nome\":\"Inq do Cliente 2\",\"documento\":\"529.982.247-25\"}"
check 201 "$CODE" "admin cria inquilino do 2o cliente"; INQ_C2=$(echo "$BODY" | jq -r '.id')
IMV3=${IDS[2]}
req POST /api/contratos "$ADMIN" "{\"imovelId\":\"$IMV3\",\"inquilinoId\":\"$INQ_C2\",\"numeroContrato\":\"CT-mix\",\"dataInicio\":\"2026-01-01\",\"diaVencimento\":10,\"valorAluguel\":1000}"
check 400 "$CODE" "contrato com imovel/inquilino de clientes diferentes -> 400"; echo "  msg=$(echo "$BODY" | jq -r '.detail')"

printf '\n===== RESULTADO: PASS=%d FAIL=%d =====\n' "$PASS" "$FAIL"
