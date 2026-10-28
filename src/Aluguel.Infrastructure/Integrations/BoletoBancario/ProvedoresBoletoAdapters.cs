using Aluguel.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aluguel.Infrastructure.Integrations.BoletoBancario;

/// <summary>Itaú Unibanco — API de cobrança (OAuth client credentials).</summary>
public sealed class ItauBoletoProvider(
    IHttpClientFactory http, IOptions<BoletoOptions> opts, ILogger<ItauBoletoProvider> log)
    : BoletoProviderBase(http, opts, log)
{
    public override string CodigoProvedor => "itau";
    protected override string NomeProvedor => "Itaú";
    protected override string CodigoBanco => "341";
    protected override string EndpointCriacaoRelativo => "cobrancas_boletos/v2/boletos";
    protected override string? UrlBasePadrao =>
        Options.UsarMock ? null : "https://api.itau.com.br/";

    protected override object MontarCorpoCriacao(BoletoGeracaoPedido p) => new
    {
        data = new
        {
            etapa_processo_boleto = "efetivacao",
            codigo_canal_operacao = "API",
            beneficiario = new
            {
                id_beneficiario = Cred(p.Credenciais, "codigo_beneficiario"),
                agencia = Cred(p.Credenciais, "agencia"),
                conta = Cred(p.Credenciais, "conta"),
                digito_conta = Cred(p.Credenciais, "digito_conta"),
            },
            dado_boleto = new
            {
                descricao_instrumento_cobranca = "boleto",
                tipo_boleto = "a vista",
                codigo_carteira = Cred(p.Credenciais, "carteira") ?? "109",
                valor_total_titulo = p.Valor.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                codigo_especie = "01",
                data_vencimento = p.DataVencimento.ToString("yyyy-MM-dd"),
                pagador = new
                {
                    pessoa = new
                    {
                        nome_pessoa = p.PagadorNome,
                        tipo_pessoa = new
                        {
                            codigo_tipo_pessoa = p.PagadorPj ? "J" : "F",
                            numero_cadastro_pessoa_fisica = p.PagadorPj ? null : p.PagadorDocumento,
                            numero_cadastro_nacional_pessoa_juridica = p.PagadorPj ? p.PagadorDocumento : null,
                        },
                    },
                },
                texto_seu_numero = p.Competencia.Replace("/", ""),
                texto_uso_beneficiario = p.Descricao,
            },
        },
    };
}

/// <summary>Bradesco — API Cobrança.</summary>
public sealed class BradescoBoletoProvider(
    IHttpClientFactory http, IOptions<BoletoOptions> opts, ILogger<BradescoBoletoProvider> log)
    : BoletoProviderBase(http, opts, log)
{
    public override string CodigoProvedor => "bradesco";
    protected override string NomeProvedor => "Bradesco";
    protected override string CodigoBanco => "237";
    protected override string EndpointCriacaoRelativo => "v1/boleto/registrar";
    protected override string? UrlBasePadrao =>
        Options.UsarMock ? null : "https://openapi.bradesco.com.br/";

    protected override object MontarCorpoCriacao(BoletoGeracaoPedido p) => new
    {
        agencia = Cred(p.Credenciais, "agencia"),
        conta = Cred(p.Credenciais, "conta"),
        carteira = Cred(p.Credenciais, "carteira") ?? "09",
        codigoBeneficiario = Cred(p.Credenciais, "codigo_beneficiario"),
        valorTitulo = p.Valor,
        dataVencimento = p.DataVencimento.ToString("dd.MM.yyyy"),
        seuNumero = p.Competencia.Replace("/", ""),
        pagador = new
        {
            nome = p.PagadorNome,
            documento = p.PagadorDocumento,
            tipoDocumento = p.PagadorPj ? "CNPJ" : "CPF",
        },
        mensagem = p.Descricao,
    };
}

/// <summary>Banco do Brasil — API Cobranças v2.</summary>
public sealed class BancoDoBrasilBoletoProvider(
    IHttpClientFactory http, IOptions<BoletoOptions> opts, ILogger<BancoDoBrasilBoletoProvider> log)
    : BoletoProviderBase(http, opts, log)
{
    public override string CodigoProvedor => "banco_do_brasil";
    protected override string NomeProvedor => "Banco do Brasil";
    protected override string CodigoBanco => "001";
    protected override string EndpointCriacaoRelativo => "cobrancas/v2/boletos";
    protected override string? UrlBasePadrao =>
        Options.UsarMock ? null : "https://api.bb.com.br/";

    protected override async Task ConfigurarAutenticacaoAsync(
        HttpClient client, IReadOnlyDictionary<string, string> credenciais, CancellationToken ct)
    {
        await base.ConfigurarAutenticacaoAsync(client, credenciais, ct);
        if (credenciais.TryGetValue("developer_application_key", out var gw) && !string.IsNullOrWhiteSpace(gw))
            client.DefaultRequestHeaders.TryAddWithoutValidation("gw-dev-app-key", gw);
    }

    protected override object MontarCorpoCriacao(BoletoGeracaoPedido p) => new
    {
        numeroConvenio = Cred(p.Credenciais, "convenio"),
        numeroCarteira = int.TryParse(Cred(p.Credenciais, "carteira"), out var cart) ? cart : 17,
        numeroVariacaoCarteira = int.TryParse(Cred(p.Credenciais, "variacao_carteira"), out var varc) ? varc : 19,
        codigoModalidade = 1,
        dataVencimento = p.DataVencimento.ToString("dd.MM.yyyy"),
        valorOriginal = p.Valor,
        numeroTituloBeneficiario = p.Competencia.Replace("/", ""),
        pagador = new
        {
            tipoInscricao = p.PagadorPj ? 2 : 1,
            numeroInscricao = long.TryParse(p.PagadorDocumento, out var doc) ? doc : 0,
            nome = p.PagadorNome,
        },
        descricaoTipoTitulo = "DM",
        indicadorAceiteTituloVencido = "S",
        mensagemBloquetoOcorrencia = p.Descricao,
    };
}

/// <summary>Santander — Workspace Cobrança.</summary>
public sealed class SantanderBoletoProvider(
    IHttpClientFactory http, IOptions<BoletoOptions> opts, ILogger<SantanderBoletoProvider> log)
    : BoletoProviderBase(http, opts, log)
{
    public override string CodigoProvedor => "santander";
    protected override string NomeProvedor => "Santander";
    protected override string CodigoBanco => "033";
    protected override string EndpointCriacaoRelativo => "collection_bill_management/v2/workspace/bank_slips";
    protected override string? UrlBasePadrao =>
        Options.UsarMock ? null : "https://trust-open.api.santander.com.br/";

    protected override object MontarCorpoCriacao(BoletoGeracaoPedido p) => new
    {
        environment = Options.Ambiente.Equals("Producao", StringComparison.OrdinalIgnoreCase) ? "PRODUCAO" : "TESTE",
        workspaceId = Cred(p.Credenciais, "workspace_id"),
        covenantCode = Cred(p.Credenciais, "convenio"),
        bankNumber = Cred(p.Credenciais, "conta"),
        dueDate = p.DataVencimento.ToString("yyyy-MM-dd"),
        nominalValue = p.Valor.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
        payer = new
        {
            name = p.PagadorNome,
            documentType = p.PagadorPj ? "CNPJ" : "CPF",
            documentNumber = p.PagadorDocumento,
        },
        yourNumber = p.Competencia.Replace("/", ""),
    };
}

/// <summary>Caixa Econômica Federal — Nova Cobrança.</summary>
public sealed class CaixaBoletoProvider(
    IHttpClientFactory http, IOptions<BoletoOptions> opts, ILogger<CaixaBoletoProvider> log)
    : BoletoProviderBase(http, opts, log)
{
    public override string CodigoProvedor => "caixa";
    protected override string NomeProvedor => "Caixa Econômica Federal";
    protected override string CodigoBanco => "104";
    protected override string EndpointCriacaoRelativo => "cobranca-bancaria/v4/boletos";
    protected override string? UrlBasePadrao =>
        Options.UsarMock ? null : "https://api.caixa.gov.br/";

    protected override object MontarCorpoCriacao(BoletoGeracaoPedido p) => new
    {
        convenio = Cred(p.Credenciais, "convenio"),
        agencia = Cred(p.Credenciais, "agencia"),
        codigoBeneficiario = Cred(p.Credenciais, "codigo_beneficiario"),
        carteira = Cred(p.Credenciais, "carteira") ?? "14",
        dataVencimento = p.DataVencimento.ToString("yyyy-MM-dd"),
        valor = p.Valor,
        numeroDocumento = p.Competencia.Replace("/", ""),
        pagador = new
        {
            nome = p.PagadorNome,
            cpfCnpj = p.PagadorDocumento,
        },
        mensagem = p.Descricao,
    };
}

/// <summary>Sicredi — API Cobrança.</summary>
public sealed class SicrediBoletoProvider(
    IHttpClientFactory http, IOptions<BoletoOptions> opts, ILogger<SicrediBoletoProvider> log)
    : BoletoProviderBase(http, opts, log)
{
    public override string CodigoProvedor => "sicredi";
    protected override string NomeProvedor => "Sicredi";
    protected override string CodigoBanco => "748";
    protected override string EndpointCriacaoRelativo => "cobranca/boleto/v1/boletos";
    protected override string? UrlBasePadrao =>
        Options.UsarMock ? null : "https://api-parceiro.sicredi.com.br/";

    protected override object MontarCorpoCriacao(BoletoGeracaoPedido p) => new
    {
        codigoBeneficiario = Cred(p.Credenciais, "codigo_beneficiario"),
        cooperativa = Cred(p.Credenciais, "cooperativa"),
        posto = Cred(p.Credenciais, "posto"),
        especieDocumento = "DUPLICATA_MERCANTIL_INDICACAO",
        dataVencimento = p.DataVencimento.ToString("yyyy-MM-dd"),
        valor = p.Valor,
        seuNumero = p.Competencia.Replace("/", ""),
        pagador = new
        {
            tipoPessoa = p.PagadorPj ? "PESSOA_JURIDICA" : "PESSOA_FISICA",
            documento = p.PagadorDocumento,
            nome = p.PagadorNome,
        },
        mensagens = string.IsNullOrWhiteSpace(p.Descricao) ? null : new[] { p.Descricao },
    };
}

/// <summary>Mercado Pago — Payments / boleto.</summary>
public sealed class MercadoPagoBoletoProvider(
    IHttpClientFactory http, IOptions<BoletoOptions> opts, ILogger<MercadoPagoBoletoProvider> log)
    : BoletoProviderBase(http, opts, log)
{
    public override string CodigoProvedor => "mercado_pago";
    protected override string NomeProvedor => "Mercado Pago";
    protected override string CodigoBanco => "";
    protected override string EndpointCriacaoRelativo => "v1/payments";
    protected override string? UrlBasePadrao =>
        Options.UsarMock ? null : "https://api.mercadopago.com/";

    protected override object MontarCorpoCriacao(BoletoGeracaoPedido p) => new
    {
        transaction_amount = p.Valor,
        description = p.Descricao ?? $"Aluguel {p.Competencia}",
        payment_method_id = "bolbradesco",
        date_of_expiration = p.DataVencimento.ToDateTime(TimeOnly.MaxValue).ToString("o"),
        payer = new
        {
            email = "pagador@example.com",
            first_name = p.PagadorNome,
            identification = new
            {
                type = p.PagadorPj ? "CNPJ" : "CPF",
                number = p.PagadorDocumento,
            },
        },
        external_reference = p.ReferenciaId.ToString("N"),
    };
}

/// <summary>Banco Inter — Cobrança v3.</summary>
public sealed class InterBoletoProvider(
    IHttpClientFactory http, IOptions<BoletoOptions> opts, ILogger<InterBoletoProvider> log)
    : BoletoProviderBase(http, opts, log)
{
    public override string CodigoProvedor => "inter";
    protected override string NomeProvedor => "Banco Inter";
    protected override string CodigoBanco => "077";
    protected override string EndpointCriacaoRelativo => "cobranca/v3/cobrancas";
    protected override string? UrlBasePadrao =>
        Options.UsarMock ? null : "https://cdpj.partners.bancointer.com.br/";

    protected override object MontarCorpoCriacao(BoletoGeracaoPedido p) => new
    {
        seuNumero = p.Competencia.Replace("/", ""),
        valorNominal = p.Valor,
        dataVencimento = p.DataVencimento.ToString("yyyy-MM-dd"),
        numDiasAgenda = 60,
        pagador = new
        {
            cpfCnpj = p.PagadorDocumento,
            tipoPessoa = p.PagadorPj ? "JURIDICA" : "FISICA",
            nome = p.PagadorNome,
        },
        mensagem = new { linha1 = p.Descricao },
        contaCorrente = Cred(p.Credenciais, "conta_corrente"),
    };
}

/// <summary>C6 Bank — API Cobrança.</summary>
public sealed class C6BoletoProvider(
    IHttpClientFactory http, IOptions<BoletoOptions> opts, ILogger<C6BoletoProvider> log)
    : BoletoProviderBase(http, opts, log)
{
    public override string CodigoProvedor => "c6";
    protected override string NomeProvedor => "C6 Bank";
    protected override string CodigoBanco => "336";
    protected override string EndpointCriacaoRelativo => "v1/bank_slips";
    protected override string? UrlBasePadrao =>
        Options.UsarMock ? null : "https://baas-api.c6bank.info/";

    protected override object MontarCorpoCriacao(BoletoGeracaoPedido p) => new
    {
        amount = p.Valor,
        due_date = p.DataVencimento.ToString("yyyy-MM-dd"),
        account_id = Cred(p.Credenciais, "account_id"),
        your_number = p.Competencia.Replace("/", ""),
        payer = new
        {
            name = p.PagadorNome,
            tax_id = p.PagadorDocumento,
        },
        description = p.Descricao,
    };
}

/// <summary>Nubank — API de cobrança (parceiros).</summary>
public sealed class NubankBoletoProvider(
    IHttpClientFactory http, IOptions<BoletoOptions> opts, ILogger<NubankBoletoProvider> log)
    : BoletoProviderBase(http, opts, log)
{
    public override string CodigoProvedor => "nubank";
    protected override string NomeProvedor => "Nubank";
    protected override string CodigoBanco => "260";
    protected override string EndpointCriacaoRelativo => "v1/boletos";
    protected override string? UrlBasePadrao =>
        Options.UsarMock ? null : "https://prod-s0-webapp-proxy.nubank.com.br/";

    protected override object MontarCorpoCriacao(BoletoGeracaoPedido p) => new
    {
        account_id = Cred(p.Credenciais, "conta_id"),
        amount = p.Valor,
        due_date = p.DataVencimento.ToString("yyyy-MM-dd"),
        payer = new
        {
            name = p.PagadorNome,
            tax_id = p.PagadorDocumento,
        },
        reference = p.Competencia,
        description = p.Descricao,
    };
}
