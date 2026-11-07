using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Assinaturas;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Common;
using MediatR;

namespace Aluguel.Application.Assinaturas.ContratarPlano;

/// <summary>
/// Contrata ou altera plano:
/// upgrade = PIX imediato e reinício de ciclo após liquidação;
/// downgrade = agendado para a próxima renovação (sem estorno/crédito).
/// </summary>
public sealed record ContratarPlanoCommand(Guid PlanoId) : IRequest<ContratarPlanoResultadoDto>;

public sealed class ContratarPlanoCommandHandler(
    IAssinaturaRepository assinaturas,
    IClienteRepository clientes,
    IPlanoRepository planos,
    IImovelRepository imoveis,
    IUsuarioService usuarios,
    IInterPixProvider pix,
    ICurrentUser currentUser,
    ICurrentTenant tenant,
    TimeProvider clock)
    : IRequestHandler<ContratarPlanoCommand, ContratarPlanoResultadoDto>
{
    public async Task<ContratarPlanoResultadoDto> Handle(
        ContratarPlanoCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.ClienteId is not { } clienteId)
            throw new InvalidOperationException("Somente gestores do cliente podem contratar plano via PIX.");

        if (!EscopoCliente.PodeAcessar(currentUser, clienteId))
            throw new InvalidOperationException("Sem permissão para contratar plano deste cliente.");

        var cliente = await clientes.ObterPorIdAsync(clienteId, cancellationToken)
            ?? throw new InvalidOperationException("Cliente não encontrado.");

        var plano = await planos.ObterPorIdAsync(request.PlanoId, cancellationToken)
            ?? throw new InvalidOperationException("Plano informado não foi encontrado.");

        if (!plano.Ativo)
            throw new InvalidOperationException("Plano inativo não pode ser contratado.");

        if (plano.Codigo == PlanoCodigo.Trial)
            throw new InvalidOperationException("O plano Trial não é contratável via pagamento.");

        if (plano.ValorMensal is null)
            throw new InvalidOperationException(
                "O plano Pro é sob consulta comercial. Entre em contato com a Lucrare para contratar.");

        if (plano.ValorMensal <= 0)
            throw new InvalidOperationException("Plano sem valor mensal não gera cobrança PIX.");

        if (cliente.PlanoId == plano.Id)
            throw new InvalidOperationException("Este plano já está vigente para o cliente.");

        await ValidarLimitesAsync(cliente, plano, cancellationToken);

        var tenantId = tenant.TenantId
            ?? throw new InvalidOperationException("Tenant não resolvido na sessão.");

        var agora = clock.GetUtcNow();
        var vigente = await assinaturas.ObterVigentePorClienteAsync(clienteId, cancellationToken);
        var planoAtual = cliente.PlanoId is { } pid
            ? await planos.ObterPorIdAsync(pid, cancellationToken)
            : null;
        var codigoAtual = planoAtual?.Codigo;

        var ehDowngrade = codigoAtual is not null
            && PlanoRanking.EhDowngrade(codigoAtual, plano.Codigo);
        var ehUpgrade = codigoAtual is null
            || codigoAtual == PlanoCodigo.Trial
            || PlanoRanking.EhUpgrade(codigoAtual, plano.Codigo);

        if (ehDowngrade)
            return await AgendarDowngradeAsync(
                vigente, cliente, plano, codigoAtual!, tenantId, agora, cancellationToken);

        if (!ehUpgrade && !PlanoRanking.MesmoNivel(codigoAtual, plano.Codigo))
            throw new InvalidOperationException("Operação de plano não reconhecida.");

        return await ContratarOuUpgradeComPixAsync(
            vigente, cliente, plano, codigoAtual, tenantId, agora, cancellationToken);
    }

    private async Task<ContratarPlanoResultadoDto> AgendarDowngradeAsync(
        Assinatura? vigente,
        Cliente cliente,
        Plano planoDestino,
        string codigoAtual,
        Guid tenantId,
        DateTimeOffset agora,
        CancellationToken ct)
    {
        if (vigente is null || vigente.Status != StatusAssinatura.Ativa)
            throw new InvalidOperationException(
                "Downgrade só pode ser agendado com assinatura ativa. Contrate um plano pago primeiro.");

        vigente.AgendarDowngrade(planoDestino.Id);

        assinaturas.AdicionarAuditoria(new AuditoriaAssinatura(
            tenantId, cliente.Id, EventoAssinatura.Downgrade,
            assinaturaId: vigente.Id,
            usuarioId: currentUser.UserId,
            planoAnterior: codigoAtual,
            novoPlano: planoDestino.Codigo,
            valor: planoDestino.ValorMensal,
            descricao:
            $"Downgrade agendado para {planoDestino.Nome} na próxima renovação " +
            $"({vigente.ProximaCobranca:yyyy-MM-dd}) — sem estorno/crédito"));

        await assinaturas.SalvarAlteracoesAsync(ct);

        return new ContratarPlanoResultadoDto(
            vigente.Id,
            PagamentoId: null,
            vigente.Status.ToString(),
            Operacao: "DowngradeAgendado",
            Mensagem:
            $"Downgrade para {planoDestino.Nome} agendado para a próxima renovação" +
            (vigente.ProximaCobranca is { } d ? $" ({d:dd/MM/yyyy})" : "") +
            ". Sem estorno ou crédito do ciclo atual.",
            Pagamento: null,
            vigente.ProximaCobranca,
            vigente.FimCicloPago,
            vigente.PlanoAgendadoId,
            planoDestino.Nome);
    }

    private async Task<ContratarPlanoResultadoDto> ContratarOuUpgradeComPixAsync(
        Assinatura? vigente,
        Cliente cliente,
        Plano plano,
        string? codigoAtual,
        Guid tenantId,
        DateTimeOffset agora,
        CancellationToken ct)
    {
        var assinaturaNova = vigente is null;
        Assinatura assinatura;
        string operacao;
        EventoAssinatura evento;

        var primeiraContratacao = vigente is null
            || vigente.Status == StatusAssinatura.Trial
            || codigoAtual is null or PlanoCodigo.Trial;

        if (vigente is null)
        {
            assinatura = new Assinatura(
                tenantId, cliente.Id, plano.Id, plano.ValorMensal!.Value, StatusAssinatura.PendentePagamento);
            assinaturas.Adicionar(assinatura);
            operacao = "Contratacao";
            evento = EventoAssinatura.Contratacao;
        }
        else if (primeiraContratacao)
        {
            assinatura = vigente;
            assinatura.IniciarPrimeiraContratacao(plano.Id, plano.ValorMensal!.Value);
            operacao = "Contratacao";
            evento = EventoAssinatura.Contratacao;
        }
        else
        {
            // Upgrade: mantém plano atual até liquidar; reinicia ciclo no webhook
            assinatura = vigente;
            assinatura.RegistrarIntencaoUpgrade();
            operacao = "Upgrade";
            evento = EventoAssinatura.Upgrade;
        }

        var (pagamento, _) = await AssinaturaPixHelper.CriarCobrancaPixAsync(
            assinatura,
            plano.Id,
            plano.ValorMensal!.Value,
            $"Assinatura Lucrare — plano {plano.Nome} ({operacao})",
            assinaturaJaRastreada: !assinaturaNova,
            assinaturas,
            pix,
            agora,
            ct);

        assinaturas.AdicionarAuditoria(new AuditoriaAssinatura(
            tenantId, cliente.Id, evento,
            assinaturaId: assinatura.Id,
            usuarioId: currentUser.UserId,
            planoAnterior: codigoAtual,
            novoPlano: plano.Codigo,
            valor: plano.ValorMensal,
            descricao: operacao == "Upgrade"
                ? $"Upgrade imediato: PIX gerado (txid {pagamento.PixTxId}) — ciclo reinicia após liquidação"
                : $"Cobrança PIX gerada (txid {pagamento.PixTxId}) — aguardando liquidação"));

        await assinaturas.SalvarAlteracoesAsync(ct);

        var beneficiario = pix.ObterBeneficiario();
        var dto = AssinaturaPixHelper.ParaDto(pagamento, cliente.Id, plano.Nome, beneficiario);

        var mensagem = operacao == "Upgrade"
            ? $"Upgrade para {plano.Nome}: pague o PIX para reiniciar o ciclo com o novo valor. Sem crédito do período anterior."
            : $"PIX gerado para o plano {plano.Nome}. A assinatura é ativada após a liquidação.";

        return new ContratarPlanoResultadoDto(
            assinatura.Id,
            pagamento.Id,
            assinatura.Status.ToString(),
            operacao,
            mensagem,
            dto,
            assinatura.ProximaCobranca,
            assinatura.FimCicloPago,
            assinatura.PlanoAgendadoId,
            null);
    }

    private async Task ValidarLimitesAsync(Cliente cliente, Plano plano, CancellationToken ct)
    {
        var imoveisAtivos = await imoveis.ContarAsync(
            cliente.Id, termo: null, tipo: null, StatusAtivoInativo.Ativo, ct);
        var usuariosAtivos = await usuarios.ContarUsuariosAtivosAsync(cliente.Id, ct);

        var motivos = new List<string>();
        if (!plano.SuportaUtilizacaoImoveis(imoveisAtivos))
        {
            motivos.Add(
                $"possui {imoveisAtivos} imóvel(is) ativo(s) e o plano {plano.Nome} permite no máximo {plano.MaxImoveis}");
        }

        if (!plano.SuportaUtilizacaoUsuarios(usuariosAtivos))
        {
            motivos.Add(
                $"possui {usuariosAtivos} usuário(s) ativo(s) e o plano {plano.Nome} permite no máximo {plano.MaxUsuarios}");
        }

        if (motivos.Count > 0)
        {
            throw new InvalidOperationException(
                $"Não é possível alterar para o plano {plano.Nome}: o cliente {string.Join("; ", motivos)}. " +
                "Adeque a utilização (inative imóveis/usuários excedentes) antes de alterar o plano.");
        }
    }
}
