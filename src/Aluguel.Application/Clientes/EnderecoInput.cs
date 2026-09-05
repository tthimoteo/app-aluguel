using Aluguel.Application.Common.Validacoes;
using Aluguel.Domain.ValueObjects;

namespace Aluguel.Application.Clientes;

/// <summary>Endereço informado em comandos de criação/atualização.</summary>
public sealed record EnderecoInput(
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? Cidade,
    string? Uf,
    string? Cep)
{
    public Endereco ParaValueObject() =>
        new(Logradouro, Numero, Complemento, Bairro, Cidade,
            Uf?.ToUpperInvariant(),
            string.IsNullOrWhiteSpace(Cep) ? null : Documento.SomenteDigitos(Cep));
}
