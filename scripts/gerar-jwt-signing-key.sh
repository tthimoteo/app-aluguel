#!/usr/bin/env bash
# Gera Jwt__SigningKey para colar no dashboard Render (não committa o valor).
set -euo pipefail
openssl rand -base64 48
echo "(cole em Jwt__SigningKey no Render — mínimo 32 caracteres)" >&2
