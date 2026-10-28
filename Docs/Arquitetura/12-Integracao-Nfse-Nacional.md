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
4. App persiste `NotaFiscalServico` (Emitida), `DocumentoFiscal` (XML autorizado + **DANFSe PDF** via QuestPDF) e gera boleto.

### DANFSe (PDF) — NT 008/2026

A API ADN de geração de DANFSe foi **desativada em 03/08/2026**. O app renderiza o PDF **localmente** a partir do **XML autorizado** da NFS-e (`INfsePdfGenerator` / `NfseDanfsePdfGenerator`), seguindo a **NT 008 v1.02**:

- Somente dados presentes no XML (sem inventar campos)
- Cabeçalho DANFSe v2.0 + logomarca NFS-e + QR Code da consulta pública
- Em produção restrita (`tpAmb=2`): faixa **“NFS-e SEM VALIDADE JURÍDICA”**
- Blocos: identificação, prestador, tomador, serviço, ISSQN (suprimido quando não incidência), IBS/CBS, totais e informações complementares (imóvel)
- Supressões permitidas (§2.3): destinatário = tomador; intermediário ausente; ISSQN não incidente

Adapter: `INfseNacionalProvider` → `SefinNfseNacionalProvider` (ou `MockNfseNacionalProvider` quando `Nfse:Ambiente=Mock`).

## Cancelamento na remoção de cobrança (§12 / UC006)

Ao **remover uma cobrança** pendente (`DELETE /api/cobrancas/{id}`):

1. Se não houver NFS-e emitida (sem chave): exclui cobrança/boleto/documentos.
2. Se a NFS-e foi **emitida há menos de 24h**: envia evento **e101101** à SEFIN (`POST /nfse/{chave}/eventos`, XML `pedRegEvento` assinado + GZIP/Base64), persiste XML do evento (`XMLCancelamento`), marca `Status=Cancelada` (motivo + protocolo) e remove o boleto.
3. Se a emissão tem **24h ou mais**: cancela a cobrança **só no sistema** (`Status=Cancelada`, boleto removido) e devolve mensagem pedindo cancelamento **manual no portal nacional da NFS-e**. A cobrança não é apagada do histórico fiscal.

Resposta JSON: `{ removida, cancelamentoNfse, mensagem }` onde `cancelamentoNfse` ∈ `SemNfseEmitida` | `CanceladaAutomaticamente` | `RequerCancelamentoManual`.

### Configuração (`Nfse`)

| Chave | Descrição |
|---|---|
| `Ambiente` | `Mock` \| `ProducaoRestrita` \| `Producao` |
| `VerAplic` | Identificador no XML (máx. 20) |
| `SeriePadrao` | Série da DPS |
| `CodigoMunicipioIbgePadrao` | IBGE fallback (São Paulo = `3550308`) |
| `CodigoTributacaoNacionalPadrao` | `cTribNac` Lucrare = **`990301`** |
| `CodigoIndicadorOperacaoIbsCbs` | `cIndOp` Lucrare = **`020201`** |
| `ClassificacaoTributariaIbsCbs` | `cClassTrib` Lucrare = **`200027`** |
| `CstIbsCbs` | CST IBS/CBS = **`200`** (3 primeiros dígitos de `cClassTrib`) |
| `PercentualTotTribSn` | `pTotTribSN` para optantes do Simples |
| `FinNfse` | Finalidade (`0` = regular); obrigatório em `infDPS` (NT 009) |
| `IncluirIbsCbs` | `true` (grupo `IBSCBS` com cIndOp/cClassTrib) |
| `EnviarImPrestador` | Envia `prest/IM` (E0120 se CNC municipal incompleto) |
| `CodigoNbsPadrao` | cNBS Lucrare residencial = **`110021000`** (comercial = `110022000` via tipo do imóvel) |

Cadastro do cliente (Minha Conta / Admin): `codigoServico` = cTribNac, `codigoIndicadorOperacao` = cIndOp, `classificacaoTributaria` = cClassTrib — cada um com **6 dígitos** (FluentValidation).

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
| E0322 | IBS/CBS sem NBS | Informar `cNBS` (locação: `110021000` / `110022000`) |
| E0932 | cIndOp de imóvel sem grupo `imovel` | Enviar `IBSCBS/imovel/gLocacao` |
| E0959 | CST ≠ 3 primeiros dígitos de `cClassTrib` | CST `200` com `cClassTrib` `200027` |
| E0121 / E0128 | `prest/xNome` ou endereço com `tpEmit=1` | Enviar só CNPJ/CPF (+ regTrib); cadastro vem do CNC |
| E0532 | `tribISSQN` ≠ 4 em serviço `99.XX.XX` | Usar `tribISSQN=4` (não incidência) para `990301` |

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
