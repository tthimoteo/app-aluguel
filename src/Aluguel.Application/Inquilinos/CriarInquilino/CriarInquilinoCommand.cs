using Aluguel.Application.Abstractions;
using Aluguel.Application.Clientes;
using Aluguel.Application.Common.Validacoes;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Common;
using Aluguel.Domain.Inquilinos;
using MediatR;

namespace Aluguel.Application.Inquilinos.CriarInquilino;

/// <summary>Cadastra um inquilino (destinatário da NFS-e). O <c>ClienteId</c> já vem resolvido pelo endpoint.</summary>
public sealed record CriarInquilinoCommand(
    Guid ClienteId,
    TipoPessoa TipoPessoa,
    string Nome,
    string Documento,
    string? InscricaoMunicipal,
    string? Telefone,
    string? Email,
    EnderecoInput? Endereco,
    Guid? ImovelId = null) : IRequest<InquilinoDto>;

public sealed class CriarInquilinoCommandHandler(
    IInquilinoRepository repositorio,
    IImovelRepository imoveis,
    ICurrentTenant tenant)
    : IRequestHandler<CriarInquilinoCommand, InquilinoDto>
{
    public async Task<InquilinoDto> Handle(CriarInquilinoCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenant.TenantId
            ?? throw new InvalidOperationException("Tenant não resolvido no token da requisição.");

        Guid? imovelId = null;
        if (request.ImovelId is { } idImovel)
        {
            var imovel = await imoveis.ObterPorIdAsync(idImovel, cancellationToken)
                ?? throw new InvalidOperationException("Imóvel não encontrado.");

            if (imovel.ClienteId != request.ClienteId)
                throw new InvalidOperationException("O imóvel não pertence ao cliente informado.");

            if (imovel.Status != StatusAtivoInativo.Ativo)
                throw new InvalidOperationException("Não é possível vincular inquilino a um imóvel inativo.");

            if (await imoveis.PossuiContratoAtivoAsync(idImovel, cancellationToken))
                throw new InvalidOperationException(
                    "Este imóvel já possui contrato ativo. Encerre o contrato atual antes de cadastrar outro inquilino.");

            if (await repositorio.ImovelPossuiInquilinoAtivoAsync(idImovel, null, cancellationToken))
                throw new InvalidOperationException(
                    "Este imóvel já possui inquilino vinculado. Remova o atual antes de incluir outro.");

            imovelId = idImovel;
        }

        var inquilino = new Inquilino(
            tenantId,
            request.ClienteId,
            request.TipoPessoa,
            request.Nome.Trim(),
            Documento.SomenteDigitos(request.Documento)!,
            request.Telefone,
            string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            request.Endereco?.ParaValueObject(),
            string.IsNullOrWhiteSpace(request.InscricaoMunicipal) ? null : request.InscricaoMunicipal.Trim(),
            imovelId);

        repositorio.Adicionar(inquilino);
        await repositorio.SalvarAlteracoesAsync(cancellationToken);

        return inquilino.ParaDto();
    }
}
