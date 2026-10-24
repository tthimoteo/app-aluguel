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

## Fluxo de emissão (implementado)

1. Prestador (cliente PJ/PF com plano NFS-e) tem certificado A1, IM, código de serviço e endereço.
2. Em `GerarCobranca` com `EmitirNfse=true`, o app carrega o PFX (`certificates` + `ISecretProtector`), monta a **DPS** (layout v1.01), assina com XMLDSig (RSA-SHA256), compacta (GZIP) e envia Base64 via `POST /nfse` na SEFIN (`dpsXmlGZipB64`).
3. SEFIN valida e devolve `chaveAcesso` + `nfseXmlGZipB64` (ou lista `erros`).
4. App persiste `NotaFiscalServico` (Emitida), `DocumentoFiscal` (XML autorizado) e gera boleto.

Adapter: `INfseNacionalProvider` → `SefinNfseNacionalProvider` (ou `MockNfseNacionalProvider` quando `Nfse:Ambiente=Mock`).

### Configuração (`Nfse`)

| Chave | Descrição |
|---|---|
| `Ambiente` | `Mock` \| `ProducaoRestrita` \| `Producao` |
| `VerAplic` | Identificador no XML (máx. 20) |
| `SeriePadrao` | Série da DPS |
| `CodigoMunicipioIbgePadrao` | IBGE fallback (São Paulo = `3550308`) |
| `CodigoTributacaoNacionalPadrao` | `cTribNac` (6 dígitos) quando o código do cliente for municipal |
| `PercentualTotTribSn` | `pTotTribSN` para optantes do Simples |
| `FinNfse` | Finalidade (`0` = regular); obrigatório em `infDPS` (NT 009) |
| `IncluirIbsCbs` | Inclui grupo `IBSCBS` (exige NBS/cIndOp válidos) |
| `EnviarImPrestador` | Envia `prest/IM` (E0120 se CNC municipal incompleto) |
| `CodigoNbsPadrao` | NBS 9 dígitos quando `IncluirIbsCbs=true` |
| `CodigoIndicadorOperacaoIbsCbs` | `cIndOp` (opcional pós-20/10/2026) |
| `CstIbsCbs` / `ClassificacaoTributariaIbsCbs` | Tributação IBS/CBS |

Em **Development**, o padrão é `ProducaoRestrita`. Em outros ambientes, `Mock` (CI sem mTLS).

### Layout DPS (produção restrita desde 20/10/2026)

A SEFIN homologa o leiaute da **NT 009**: `finNFSe` fica em `infDPS` (antes de `cLocEmi`), não dentro de `IBSCBS`. CST e `cClassTrib` sobem para `IBSCBS/valores/trib`.

### Homologação — rejeições típicas (pipeline ok)

| Código | Significado | Ação |
|---|---|---|
| E0015 | Competência futura vs `dhEmi` | Emitir só no mês da competência (ou anterior) |
| E0120 | IM sem CNC complementar | `EnviarImPrestador=false` até cadastrar no CNC |
| E0312 | `cTribNac` não administrado pelo município | Ajustar código nacional conveniado em SP / município |
| E0207 | CPF do tomador inexistente na RFB | Usar documento real do inquilino |
| E0322 | IBS/CBS sem NBS | Informar `cNBS` ou desligar `IncluirIbsCbs` |

O adapter autentica (mTLS A1), assina a DPS e fala com a SEFIN; autorização plena depende do CNC, lista de serviços do município e documentos do tomador.

## Cliente Demo (sandbox)

Para testes locais/homologação, o seed Development garante o emitente:

| Campo | Valor |
|---|---|
| Id | `e6c2a056-b096-4b51-852c-278a460bea4b` |
| Tipo | PJ |
| CNPJ | `15.809.930/0001-68` (`15809930000168`) |
| Razão social | `T THIMOTEO CONSULTORIA E SERVICOS DE ENGENHARIA LTDA` |
| Nome fantasia | `Cliente Demo` |

Pré-requisitos cadastrais (já preenchíveis pela UI): inscrição municipal, endereço completo, código de serviço, regime tributário e certificado A1 ativo.
