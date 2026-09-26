namespace Aluguel.Application.Clientes;

/// <summary>Metadados públicos do certificado A1 — nunca inclui senha nem bytes do arquivo.</summary>
public sealed record CertificadoDigitalDto(
    Guid Id,
    Guid ClienteId,
    string Thumbprint,
    DateTimeOffset Validade,
    bool Ativo,
    bool Vencido,
    DateTimeOffset CreatedAt);
