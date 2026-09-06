using Aluguel.Application.Abstractions;
using Aluguel.Application.Clientes;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Common;
using Aluguel.Domain.Imoveis;
using MediatR;

namespace Aluguel.Application.Imoveis.AtualizarImovel;

public sealed record AtualizarImovelCommand(
    Guid Id,
    string Nome,
    TipoImovel Tipo,
    string? NumeroIptu,
    string? NumeroMatricula,
    StatusAtivoInativo Status,
    EnderecoInput? Endereco) : IRequest<ImovelDto?>;

public sealed class AtualizarImovelCommandHandler(IImovelRepository repositorio, ICurrentUser currentUser)
    : IRequestHandler<AtualizarImovelCommand, ImovelDto?>
{
    public async Task<ImovelDto?> Handle(AtualizarImovelCommand request, CancellationToken cancellationToken)
    {
        var imovel = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        if (imovel is null || !EscopoCliente.PodeAcessar(currentUser, imovel.ClienteId))
            return null;

        imovel.Atualizar(
            request.Nome.Trim(),
            request.Tipo,
            request.Endereco?.ParaValueObject(),
            string.IsNullOrWhiteSpace(request.NumeroIptu) ? null : request.NumeroIptu.Trim(),
            string.IsNullOrWhiteSpace(request.NumeroMatricula) ? null : request.NumeroMatricula.Trim());

        if (request.Status == StatusAtivoInativo.Ativo && imovel.Status != StatusAtivoInativo.Ativo)
        {
            // Reativar consome uma vaga do plano (imóveis inativos não contam no limite).
            if (!await repositorio.PodeAdicionarImovelAsync(imovel.ClienteId, cancellationToken))
                throw new InvalidOperationException("Limite de imóveis do plano atingido.");
            imovel.Ativar();
        }
        else if (request.Status == StatusAtivoInativo.Inativo)
        {
            imovel.Inativar();
        }

        await repositorio.SalvarAlteracoesAsync(cancellationToken);
        return imovel.ParaDto();
    }
}
