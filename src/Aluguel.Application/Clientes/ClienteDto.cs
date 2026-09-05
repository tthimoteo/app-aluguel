namespace Aluguel.Application.Clientes;

public sealed record EnderecoDto(
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? Cidade,
    string? Uf,
    string? Cep);

/// <summary>Representação de leitura de um cliente (resposta da API).</summary>
public sealed record ClienteDto(
    Guid Id,
    string TipoPessoa,
    Guid TenantId,
    Guid? PlanoId,
    string Status,
    string NomeExibicao,
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
    EnderecoDto Endereco,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Página de resultados para a listagem.</summary>
public sealed record PaginaDto<T>(IReadOnlyList<T> Itens, int Total, int Skip, int Take);
