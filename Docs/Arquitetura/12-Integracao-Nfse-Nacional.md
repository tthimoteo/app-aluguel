# Integração NFS-e — API Nacional (Produção Restrita / Sandbox)

Rastreabilidade: especificação §11 (faturamento / NFS-e); sprints em `09-Plano-de-Implementacao-Sprints.md` (adapter `INfseNacionalProvider`).

## Ambientes oficiais

| Papel | Produção restrita (sandbox) | Produção |
|---|---|---|
| **SEFIN Nacional** — recepção de DPS e geração síncrona da NFS-e | https://sefin.producaorestrita.nfse.gov.br/API/SefinNacional/docs/index | https://sefin.nfse.gov.br/SefinNacional/docs/index |
| **ADN Contribuintes** — consulta/distribuição de DF-e e eventos | https://adn.producaorestrita.nfse.gov.br/contribuintes/docs/index.html | https://adn.nfse.gov.br/contribuintes/docs/index.html |
| Índice geral de APIs | https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/apis-prod-restrita-e-producao | idem |

> A emissão (POST DPS → NFS-e) é feita na **SEFIN Nacional**, com certificado A1 do contribuinte (mTLS).  
> O link ADN `/contribuintes` serve para **consulta** (NSU, eventos), não para gerar a nota.

## Fluxo de emissão (alvo)

1. Prestador (cliente PJ/PF com plano NFS-e) tem certificado A1 cadastrado.
2. App monta o XML da **DPS**, assina com o A1, compacta (GZIP) e envia em Base64 via `POST /nfse` na SEFIN.
3. SEFIN valida e devolve XML da NFS-e ou rejeição.
4. App persiste `NotaFiscalServico` (Emitida), `DocumentoFiscal` (XML/PDF/DANFSe) e gera boleto quando aplicável.

## Cliente Demo (sandbox)

Para testes locais/homologação, o seed Development garante o emitente:

| Campo | Valor |
|---|---|
| Id | `e6c2a056-b096-4b51-852c-278a460bea4b` |
| Tipo | PJ |
| CNPJ | `15.809.930/0001-68` (`15809930000168`) |
| Razão social | `T THIMOTEO CONSULTORIA E SERVICOS DE ENGENHARIA LTDA` |
| Nome fantasia | `Cliente Demo` |

O certificado A1 já pode ser enviado pela tela Minha Conta / Admin do cliente.
