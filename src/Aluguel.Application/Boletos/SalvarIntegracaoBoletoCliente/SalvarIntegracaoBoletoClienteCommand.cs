using System.Text.Json;
using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Financeiro;
using MediatR;

namespace Aluguel.Application.Boletos.SalvarIntegracaoBoletoCliente;

public sealed record SalvarIntegracaoBoletoClienteCommand(
    Guid ClienteId,
    string ProvedorCodigo,
    IReadOnlyDictionary<string, string?> Campos) : IRequest<IntegracaoBoletoClienteDto>;

public sealed class SalvarIntegracaoBoletoClienteCommandHandler(
    IIntegracaoBoletoRepository integracoes,
    IClienteRepository clientes,
    IPlanoRepository planos,
    ISecretProtector protector,
    ICurrentUser currentUser)
    : IRequestHandler<SalvarIntegracaoBoletoClienteCommand, IntegracaoBoletoClienteDto>
{
    public async Task<IntegracaoBoletoClienteDto> Handle(
        SalvarIntegracaoBoletoClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await clientes.ObterPorIdAsync(request.ClienteId, cancellationToken)
            ?? throw new InvalidOperationException("Cliente não encontrado.");

        if (!EscopoCliente.PodeAcessar(currentUser, cliente.Id))
            throw new InvalidOperationException("Sem permissão para alterar a integração de boleto deste cliente.");

        if (cliente.PlanoId is null)
            throw new InvalidOperationException("O cliente precisa de um plano com módulo de cobrança para cadastrar boletos.");

        var plano = await planos.ObterPorIdAsync(cliente.PlanoId.Value, cancellationToken)
            ?? throw new InvalidOperationException("Plano do cliente não encontrado.");

        if (!plano.PermiteModuloCobranca && !currentUser.EhAdministrador)
            throw new InvalidOperationException(
                "O plano contratado não permite emissão de boletos. Faça upgrade do plano para habilitar.");

        var def = ProvedorBoletoCatalogo.Obter(request.ProvedorCodigo)
            ?? throw new InvalidOperationException(
                $"Provedor de boleto '{request.ProvedorCodigo}' não é suportado. Consulte o catálogo.");

        var existente = await integracoes.ObterAtivaPorClienteAsync(cliente.Id, cancellationToken);
        var segredosAnteriores = existente is null
            ? new Dictionary<string, string>()
            : ObterIntegracaoBoletoCliente.ObterIntegracaoBoletoClienteQueryHandler
                .DescriptografarSegredos(existente.SegredosCifrados, protector);

        var publicos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var segredos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var campo in def.Campos)
        {
            request.Campos.TryGetValue(campo.Chave, out var bruto);
            var valor = bruto?.Trim() ?? "";

            if (campo.Tipo == TipoCampoProvedorBoleto.Segredo)
            {
                if (string.IsNullOrEmpty(valor))
                {
                    if (segredosAnteriores.TryGetValue(campo.Chave, out var antigo) && !string.IsNullOrEmpty(antigo))
                        valor = antigo;
                    else if (campo.Obrigatorio)
                        throw new InvalidOperationException($"Informe o campo obrigatório: {campo.Rotulo}.");
                }

                if (!string.IsNullOrEmpty(valor))
                    segredos[campo.Chave] = valor;
            }
            else
            {
                if (campo.Obrigatorio && string.IsNullOrEmpty(valor))
                    throw new InvalidOperationException($"Informe o campo obrigatório: {campo.Rotulo}.");
                if (!string.IsNullOrEmpty(valor))
                    publicos[campo.Chave] = valor;
            }
        }

        if (segredos.Count == 0)
            throw new InvalidOperationException("Informe ao menos um segredo de autenticação da API do banco.");

        var publicosJson = JsonSerializer.Serialize(publicos);
        var segredosJson = JsonSerializer.Serialize(segredos);
        var cifrados = protector.Protect(segredosJson);

        if (existente is null)
        {
            existente = new IntegracaoBoletoCliente(
                cliente.TenantId, cliente.Id, def.Codigo, publicosJson, cifrados);
            integracoes.Adicionar(existente);
        }
        else
        {
            existente.Atualizar(def.Codigo, publicosJson, cifrados);
        }

        await integracoes.SalvarAlteracoesAsync(cancellationToken);

        var chavesSegredo = segredos.Keys.ToList();
        return new IntegracaoBoletoClienteDto(
            existente.Id,
            existente.ClienteId,
            existente.ProvedorCodigo,
            def.Nome,
            existente.Ativo,
            publicos,
            chavesSegredo,
            existente.UpdatedAt);
    }
}
