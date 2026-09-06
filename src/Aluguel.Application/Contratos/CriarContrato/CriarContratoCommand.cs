using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Contratos;
using MediatR;

namespace Aluguel.Application.Contratos.CriarContrato;

/// <summary>
/// Cadastra um contrato de aluguel (UC004). O cliente é derivado do imóvel; o imóvel e o inquilino
/// devem pertencer ao mesmo cliente e o imóvel não pode ter contrato ativo (CASO 3).
/// </summary>
public sealed record CriarContratoCommand(
    Guid ImovelId,
    Guid InquilinoId,
    string NumeroContrato,
    DateOnly DataInicio,
    DateOnly? DataFimPrevista,
    int DiaVencimento,
    decimal ValorAluguel,
    decimal? JurosAtrasoPct,
    decimal? MultaAtrasoPct) : IRequest<ContratoDto>;

public sealed class CriarContratoCommandHandler(
    IContratoRepository contratos,
    IImovelRepository imoveis,
    IInquilinoRepository inquilinos,
    ICurrentTenant tenant,
    ICurrentUser currentUser)
    : IRequestHandler<CriarContratoCommand, ContratoDto>
{
    public async Task<ContratoDto> Handle(CriarContratoCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenant.TenantId
            ?? throw new InvalidOperationException("Tenant não resolvido no token da requisição.");

        var imovel = await imoveis.ObterPorIdAsync(request.ImovelId, cancellationToken);
        if (imovel is null || !EscopoCliente.PodeAcessar(currentUser, imovel.ClienteId))
            throw new InvalidOperationException("Imóvel não encontrado.");

        var inquilino = await inquilinos.ObterPorIdAsync(request.InquilinoId, cancellationToken);
        if (inquilino is null || !EscopoCliente.PodeAcessar(currentUser, inquilino.ClienteId))
            throw new InvalidOperationException("Inquilino não encontrado.");

        if (inquilino.ClienteId != imovel.ClienteId)
            throw new InvalidOperationException("Imóvel e inquilino devem pertencer ao mesmo cliente.");

        // CASO 3: apenas um contrato ativo por imóvel (⇒ um inquilino por vez).
        if (await contratos.ImovelPossuiContratoAtivoAsync(imovel.Id, null, cancellationToken))
            throw new InvalidOperationException("O imóvel já possui um contrato ativo.");

        var contrato = new Contrato(
            tenantId,
            imovel.ClienteId,
            imovel.Id,
            inquilino.Id,
            request.NumeroContrato.Trim(),
            request.DataInicio,
            request.DiaVencimento,
            request.ValorAluguel,
            request.DataFimPrevista,
            request.JurosAtrasoPct,
            request.MultaAtrasoPct);

        contratos.Adicionar(contrato);
        await contratos.SalvarAlteracoesAsync(cancellationToken);

        return contrato.ParaDto();
    }
}
