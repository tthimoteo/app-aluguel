using Aluguel.Domain.Clientes;

namespace Aluguel.Application.Clientes;

internal static class ClienteMappings
{
    public static ClienteDto ParaDto(this Cliente c) => new(
        c.Id,
        c.TipoPessoa.ToString(),
        c.TenantId,
        c.PlanoId,
        c.Status.ToString(),
        c.NomeExibicao,
        c.Nome,
        c.Cpf,
        c.DataNascimento,
        c.RazaoSocial,
        c.NomeFantasia,
        c.Cnpj,
        c.InscricaoMunicipal,
        c.CnaePrincipal,
        c.Telefone,
        c.Email,
        new EnderecoDto(
            c.Endereco.Logradouro, c.Endereco.Numero, c.Endereco.Complemento,
            c.Endereco.Bairro, c.Endereco.Cidade, c.Endereco.Uf, c.Endereco.Cep),
        c.CreatedAt,
        c.UpdatedAt);
}
