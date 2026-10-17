using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Imoveis;
using MediatR;

namespace Aluguel.Application.Imoveis.DefinirCobrancaImovel;

/// <summary>Atualiza o cadastro de cobrança do imóvel (entre dados básicos e inquilino).</summary>
public sealed record DefinirCobrancaImovelCommand(
    Guid Id,
    string? CompetenciaInicial,
    PropositoLocacao? PropositoLocacao,
    int? DiaVencimentoCobranca,
    decimal? DespesasCondominiais,
    decimal? ValorIptu) : IRequest<ImovelDto?>;

public sealed class DefinirCobrancaImovelCommandHandler(IImovelRepository repositorio, ICurrentUser currentUser)
    : IRequestHandler<DefinirCobrancaImovelCommand, ImovelDto?>
{
    public async Task<ImovelDto?> Handle(DefinirCobrancaImovelCommand request, CancellationToken cancellationToken)
    {
        var imovel = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        if (imovel is null || !EscopoCliente.PodeAcessar(currentUser, imovel.ClienteId))
            return null;

        var competencia = string.IsNullOrWhiteSpace(request.CompetenciaInicial)
            ? null
            : request.CompetenciaInicial.Trim();

        imovel.DefinirCobranca(
            competencia,
            request.PropositoLocacao,
            request.DiaVencimentoCobranca,
            request.DespesasCondominiais,
            request.ValorIptu);

        await repositorio.SalvarAlteracoesAsync(cancellationToken);
        return imovel.ParaDto();
    }
}
