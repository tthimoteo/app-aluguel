using Aluguel.Application.Clientes;
using Aluguel.Domain.Inquilinos;

namespace Aluguel.Application.Inquilinos;

internal static class InquilinoMappings
{
    public static InquilinoDto ParaDto(this Inquilino i) => new(
        i.Id,
        i.TenantId,
        i.ClienteId,
        i.TipoPessoa.ToString(),
        i.Nome,
        i.Documento,
        i.InscricaoMunicipal,
        i.Telefone,
        i.Email,
        i.Status.ToString(),
        new EnderecoDto(
            i.Endereco.Logradouro, i.Endereco.Numero, i.Endereco.Complemento,
            i.Endereco.Bairro, i.Endereco.Cidade, i.Endereco.Uf, i.Endereco.Cep),
        i.CreatedAt,
        i.UpdatedAt);
}
