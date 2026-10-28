using Aluguel.Application.Abstractions;
using Aluguel.Application.Clientes.EnviarCertificadoCliente;
using Aluguel.Application.Common.Acesso;
using Aluguel.Application.Cobrancas.BaixarArquivoCobranca;
using Aluguel.Domain.Fiscal;
using MediatR;

namespace Aluguel.Application.Cobrancas.RemoverCobranca;

public sealed record RemoverCobrancaCommand(Guid Id) : IRequest<RemoverCobrancaResultado?>;

public sealed class RemoverCobrancaCommandHandler(
    ICobrancaRepository cobrancas,
    IClienteRepository clientes,
    IPlanoRepository planos,
    ICertificadoDigitalRepository certificados,
    IFileStorage storage,
    ISecretProtector protector,
    INfseNacionalProvider nfseProvider,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<RemoverCobrancaCommand, RemoverCobrancaResultado?>
{
    public async Task<RemoverCobrancaResultado?> Handle(
        RemoverCobrancaCommand request, CancellationToken cancellationToken)
    {
        var nfse = await cobrancas.ObterNfseAsync(request.Id, cancellationToken);
        if (nfse is null)
            return null;

        if (!EscopoCliente.PodeAcessar(currentUser, nfse.ClienteId))
            throw new InvalidOperationException("Sem permissão para remover esta cobrança.");

        await AcessoCobranca.GarantirPlanoPermiteAsync(
            currentUser, clientes, planos, nfse.ClienteId, cancellationToken);

        if (await cobrancas.ObterPagamentoPorNfseAsync(nfse.Id, cancellationToken) is not null)
            throw new InvalidOperationException(
                "Cobrança já paga não pode ser removida. Somente cobranças pendentes de pagamento podem ser excluídas.");

        if (nfse.Status == StatusNfse.Cancelada)
            throw new InvalidOperationException("Esta cobrança já está cancelada.");

        var agora = timeProvider.GetUtcNow();
        var temNfseEmitida = nfse.Status == StatusNfse.Emitida
                             && !string.IsNullOrWhiteSpace(nfse.ChaveAcesso)
                             && nfse.DataEmissao is not null;

        if (!temNfseEmitida)
        {
            await cobrancas.RemoverCobrancaAsync(nfse.Id, cancellationToken);
            await cobrancas.SalvarAlteracoesAsync(cancellationToken);
            return new RemoverCobrancaResultado(
                true, NfseCancelamentoAoRemover.SemNfseEmitida,
                NfseCancelamentoPolitica.MensagemSemNfseEmitida);
        }

        if (NfseCancelamentoPolitica.DentroDoPrazoAutomatico(nfse.DataEmissao, agora))
        {
            await CancelarNaSefinEPersistirAsync(nfse, agora, cancellationToken);
            await cobrancas.RemoverBoletosDaNfseAsync(nfse.Id, cancellationToken);
            await cobrancas.SalvarAlteracoesAsync(cancellationToken);
            return new RemoverCobrancaResultado(
                true, NfseCancelamentoAoRemover.CanceladaAutomaticamente,
                NfseCancelamentoPolitica.MensagemCanceladaAutomaticamente);
        }

        nfse.MarcarCancelada(
            NfseCancelamentoPolitica.MotivoRemocaoCobranca +
            " Cancelamento fiscal pendente no portal nacional da NFS-e (prazo de 24h ultrapassado).",
            protocolo: null,
            agora);
        await cobrancas.RemoverBoletosDaNfseAsync(nfse.Id, cancellationToken);
        await cobrancas.SalvarAlteracoesAsync(cancellationToken);
        return new RemoverCobrancaResultado(
            true, NfseCancelamentoAoRemover.RequerCancelamentoManual,
            NfseCancelamentoPolitica.MensagemRequerCancelamentoManual);
    }

    private async Task CancelarNaSefinEPersistirAsync(
        NotaFiscalServico nfse, DateTimeOffset agora, CancellationToken ct)
    {
        var cliente = await clientes.ObterPorIdAsync(nfse.ClienteId, ct)
                      ?? throw new InvalidOperationException("Cliente emitente não encontrado.");

        var cert = await certificados.ObterAtivoPorClienteAsync(nfse.ClienteId, ct)
                   ?? throw new InvalidOperationException(
                       "Envie o certificado digital A1 do cliente para cancelar a NFS-e automaticamente (prazo de 24h).");

        if (cert.Validade < agora)
            throw new InvalidOperationException("O certificado digital A1 do cliente está vencido.");

        await using var pfxStream = await storage.DownloadAsync(
            EnviarCertificadoClienteCommandHandler.Bucket, cert.StoragePath, ct);
        await using var pfxBuffer = new MemoryStream();
        await pfxStream.CopyToAsync(pfxBuffer, ct);
        var pfxBytes = pfxBuffer.ToArray();
        if (pfxBytes.Length == 0)
            throw new InvalidOperationException("Arquivo do certificado A1 está vazio no storage.");

        string senha;
        try
        {
            senha = protector.Unprotect(cert.SenhaCifrada);
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Não foi possível recuperar a senha do certificado A1.");
        }

        var docAutor = cliente.TipoPessoa == Domain.Clientes.TipoPessoa.PJ
            ? cliente.Cnpj!
            : cliente.Cpf!;

        var resultado = await nfseProvider.CancelarAsync(
            new NfseCancelamentoPedido(
                ChaveAcesso: nfse.ChaveAcesso!,
                DocumentoAutor: docAutor,
                AutorPessoaJuridica: cliente.TipoPessoa == Domain.Clientes.TipoPessoa.PJ,
                Motivo: NfseCancelamentoPolitica.MotivoRemocaoCobranca,
                CodigoMotivo: NfseCancelamentoPolitica.CodigoMotivoErroEmissao,
                CertificadoPfx: pfxBytes,
                SenhaCertificado: senha),
            ct);

        if (!resultado.Sucesso)
            throw new InvalidOperationException(
                resultado.MensagemErro
                ?? "A SEFIN rejeitou o cancelamento da NFS-e. A cobrança não foi removida.");

        nfse.MarcarCancelada(
            NfseCancelamentoPolitica.MotivoRemocaoCobranca,
            resultado.Protocolo,
            agora);

        if (resultado.XmlEvento is { Length: > 0 })
        {
            var rel = $"{nfse.TenantId:D}/{nfse.Id:D}-cancelamento.xml";
            string path;
            await using (var s = new MemoryStream(resultado.XmlEvento))
            {
                path = await storage.UploadAsync(
                    BaixarArquivoCobrancaQueryHandler.BucketNfseXml, rel, s, "application/xml", ct);
            }

            cobrancas.AdicionarDocumento(new DocumentoFiscal(
                nfse.TenantId, nfse.Id, TipoDocumentoFiscal.XMLCancelamento, path,
                tamanhoBytes: resultado.XmlEvento.Length));
        }
    }
}
