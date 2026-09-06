using Aluguel.Application.Clientes;

namespace Aluguel.Application.Inquilinos;

/// <summary>Representação de leitura de um inquilino (resposta da API).</summary>
public sealed record InquilinoDto(
    Guid Id,
    Guid TenantId,
    Guid ClienteId,
    string TipoPessoa,
    string Nome,
    string Documento,
    string? InscricaoMunicipal,
    string? Telefone,
    string? Email,
    string Status,
    EnderecoDto Endereco,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
