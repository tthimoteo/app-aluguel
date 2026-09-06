using Aluguel.Application.Clientes;
using Aluguel.Domain.Imoveis;

namespace Aluguel.Application.Imoveis;

internal static class ImovelMappings
{
    public static ImovelDto ParaDto(this Imovel i) => new(
        i.Id,
        i.TenantId,
        i.ClienteId,
        i.Nome,
        i.Tipo.ToString(),
        i.NumeroIptu,
        i.NumeroMatricula,
        i.Status.ToString(),
        new EnderecoDto(
            i.Endereco.Logradouro, i.Endereco.Numero, i.Endereco.Complemento,
            i.Endereco.Bairro, i.Endereco.Cidade, i.Endereco.Uf, i.Endereco.Cep),
        i.CreatedAt,
        i.UpdatedAt);
}
