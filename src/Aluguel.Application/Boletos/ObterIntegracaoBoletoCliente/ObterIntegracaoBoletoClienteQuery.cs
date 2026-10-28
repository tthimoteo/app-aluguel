using System.Text.Json;
using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Boletos.ObterIntegracaoBoletoCliente;

public sealed record ObterIntegracaoBoletoClienteQuery(Guid ClienteId) : IRequest<IntegracaoBoletoClienteDto?>;

public sealed class ObterIntegracaoBoletoClienteQueryHandler(
    IIntegracaoBoletoRepository integracoes,
    IClienteRepository clientes,
    ISecretProtector protector,
    ICurrentUser currentUser)
    : IRequestHandler<ObterIntegracaoBoletoClienteQuery, IntegracaoBoletoClienteDto?>
{
    public async Task<IntegracaoBoletoClienteDto?> Handle(
        ObterIntegracaoBoletoClienteQuery request, CancellationToken cancellationToken)
    {
        var cliente = await clientes.ObterPorIdAsync(request.ClienteId, cancellationToken);
        if (cliente is null || !EscopoCliente.PodeAcessar(currentUser, cliente.Id))
            return null;

        var integ = await integracoes.ObterAtivaPorClienteAsync(request.ClienteId, cancellationToken);
        if (integ is null) return null;

        var def = ProvedorBoletoCatalogo.Obter(integ.ProvedorCodigo);
        var segredos = DescriptografarSegredos(integ.SegredosCifrados, protector);
        var chavesSegredo = def?.Campos
            .Where(c => c.Tipo == TipoCampoProvedorBoleto.Segredo)
            .Select(c => c.Chave)
            .Where(k => segredos.ContainsKey(k) && !string.IsNullOrWhiteSpace(segredos[k]))
            .ToList() ?? segredos.Keys.ToList();

        return new IntegracaoBoletoClienteDto(
            integ.Id,
            integ.ClienteId,
            integ.ProvedorCodigo,
            def?.Nome ?? integ.ProvedorCodigo,
            integ.Ativo,
            integ.LerConfiguracaoPublica(),
            chavesSegredo,
            integ.UpdatedAt);
    }

    internal static Dictionary<string, string> DescriptografarSegredos(
        byte[] cifrados, ISecretProtector protector)
    {
        try
        {
            var json = protector.Unprotect(cifrados);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }
}
