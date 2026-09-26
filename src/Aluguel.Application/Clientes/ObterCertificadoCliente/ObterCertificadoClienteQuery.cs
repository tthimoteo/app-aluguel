using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Clientes.ObterCertificadoCliente;

public sealed record ObterCertificadoClienteQuery(Guid ClienteId) : IRequest<CertificadoDigitalDto?>;

public sealed class ObterCertificadoClienteQueryHandler(
    ICertificadoDigitalRepository certificados,
    IClienteRepository clientes,
    ICurrentUser currentUser) : IRequestHandler<ObterCertificadoClienteQuery, CertificadoDigitalDto?>
{
    public async Task<CertificadoDigitalDto?> Handle(
        ObterCertificadoClienteQuery request, CancellationToken cancellationToken)
    {
        var cliente = await clientes.ObterPorIdAsync(request.ClienteId, cancellationToken);
        if (cliente is null || !EscopoCliente.PodeAcessar(currentUser, cliente.Id))
            return null;

        var cert = await certificados.ObterAtivoPorClienteAsync(request.ClienteId, cancellationToken);
        return cert?.ParaDto();
    }
}
