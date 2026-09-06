using Aluguel.Application.Clientes;

namespace Aluguel.Application.Imoveis;

/// <summary>Representação de leitura de um imóvel (resposta da API).</summary>
public sealed record ImovelDto(
    Guid Id,
    Guid TenantId,
    Guid ClienteId,
    string Nome,
    string Tipo,
    string? NumeroIptu,
    string? NumeroMatricula,
    string Status,
    EnderecoDto Endereco,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
