using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Validacoes;
using Aluguel.Domain.Clientes;
using MediatR;

namespace Aluguel.Application.Clientes.CriarCliente;

public sealed record CriarClienteCommand(
    TipoPessoa TipoPessoa,
    Guid? PlanoId,
    string? Nome,
    string? Cpf,
    DateOnly? DataNascimento,
    string? RazaoSocial,
    string? NomeFantasia,
    string? Cnpj,
    string? InscricaoMunicipal,
    string? CnaePrincipal,
    string? Telefone,
    string? Email,
    EnderecoInput? Endereco) : IRequest<ClienteDto>;

public sealed class CriarClienteCommandHandler(IClienteRepository repositorio, ICurrentTenant tenant)
    : IRequestHandler<CriarClienteCommand, ClienteDto>
{
    public async Task<ClienteDto> Handle(CriarClienteCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenant.TenantId
            ?? throw new InvalidOperationException("Tenant não resolvido no token da requisição.");

        var endereco = request.Endereco?.ParaValueObject();

        var cliente = request.TipoPessoa == TipoPessoa.PF
            ? Cliente.CriarPessoaFisica(tenantId, request.PlanoId, request.Nome!,
                Documento.SomenteDigitos(request.Cpf)!, request.DataNascimento,
                request.Telefone, request.Email, endereco)
            : Cliente.CriarPessoaJuridica(tenantId, request.PlanoId, request.RazaoSocial!, request.NomeFantasia,
                Documento.SomenteDigitos(request.Cnpj)!, request.InscricaoMunicipal, request.CnaePrincipal,
                request.Telefone, request.Email, endereco);

        repositorio.Adicionar(cliente);
        await repositorio.SalvarAlteracoesAsync(cancellationToken);

        return cliente.ParaDto();
    }
}
