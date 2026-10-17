using Aluguel.Domain.Fiscal;

namespace Aluguel.Application.Clientes;

internal static class CertificadoMappings
{
    public static CertificadoDigitalDto ParaDto(this CertificadoDigital c) => new(
        c.Id,
        c.ClienteId,
        c.Thumbprint,
        c.Validade,
        c.Ativo,
        Vencido: c.Validade < DateTimeOffset.UtcNow,
        c.CreatedAt);
}
