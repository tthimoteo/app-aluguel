using Aluguel.Application.Abstractions;
using Aluguel.Domain.Clientes;
using MediatR;

namespace Aluguel.Application.Clientes.AtualizarCliente;

/// <summary>Atualiza dados cadastrais. Tipo de pessoa e CPF/CNPJ são imutáveis.</summary>
public sealed record AtualizarClienteCommand(
    Guid Id,
    Guid? PlanoId,
    string? Nome,
    DateOnly? DataNascimento,
    string? RazaoSocial,
    string? NomeFantasia,
    string? InscricaoMunicipal,
    string? CnaePrincipal,
    string? Telefone,
    string? Email,
    EnderecoInput? Endereco) : IRequest<ClienteDto?>;

public sealed class AtualizarClienteCommandHandler(IClienteRepository repositorio)
    : IRequestHandler<AtualizarClienteCommand, ClienteDto?>
{
    public async Task<ClienteDto?> Handle(AtualizarClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        if (cliente is null)
            return null;

        var endereco = request.Endereco?.ParaValueObject();

        if (cliente.TipoPessoa == TipoPessoa.PF)
            cliente.AtualizarDadosPessoaFisica(request.Nome!, request.DataNascimento,
                request.Telefone, request.Email, endereco);
        else
            cliente.AtualizarDadosPessoaJuridica(request.RazaoSocial!, request.NomeFantasia,
                request.InscricaoMunicipal, request.CnaePrincipal, request.Telefone, request.Email, endereco);

        if (request.PlanoId is not null)
            cliente.DefinirPlano(request.PlanoId);

        await repositorio.SalvarAlteracoesAsync(cancellationToken);
        return cliente.ParaDto();
    }
}
