using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Assinaturas;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Common;
using MediatR;

namespace Aluguel.Application.Assinaturas.ContratarPlano;

/// <summary>
/// Contrata/troca plano: cria cobrança PIX no Banco Inter e deixa a assinatura
/// em <c>PendentePagamento</c> até o webhook de liquidação (§3).
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
        var planoAnteriorCodigo = await ResolverCodigoPlanoAsync(cliente.PlanoId, cancellationToken);
        var assinaturaNova = vigente is null;

        Assinatura assinatura;
        if (vigente is null)
        {
            assinatura = new Assinatura(
                tenantId, clienteId, plano.Id, plano.ValorMensal.Value, StatusAssinatura.PendentePagamento);
            assinaturas.Adicionar(assinatura);
        }
        else
        {
            assinatura = vigente;
            assinatura.SolicitarContratacao(plano.Id, plano.ValorMensal.Value);
        }

        var txId = GerarTxId();
        var pagamento = assinatura.CriarPagamentoPix(
            plano.Id, plano.ValorMensal.Value, agora.AddHours(1), txId);
        // Assinatura já rastreada: coleção IReadOnly não propaga INSERT — adiciona explicitamente.
        if (!assinaturaNova)
            assinaturas.AdicionarPagamento(pagamento);

        var cob = await pix.CriarCobrancaImediataAsync(
            new InterPixCobrancaPedido(
                TxId: txId,
                Valor: plano.ValorMensal.Value,
                SolicitacaoPagador: $"Assinatura Lucrare — plano {plano.Nome}"),
            cancellationToken);

        if (!cob.Sucesso || string.IsNullOrWhiteSpace(cob.PixCopiaCola))
        {
            pagamento.Cancelar(cob.MensagemErro ?? "Falha ao criar cobrança PIX no Banco Inter");
            await assinaturas.SalvarAlteracoesAsync(cancellationToken);
            throw new InvalidOperationException(
                cob.MensagemErro ?? "Não foi possível gerar o QR Code PIX. Tente novamente em instantes.");
        }

        pagamento.RegistrarQrCode(cob.PixCopiaCola, cob.QrCodeBase64, cob.Location);

        var evento = planoAnteriorCodigo is null or PlanoCodigo.Trial
            ? EventoAssinatura.Contratacao
            : CompararPlanos(planoAnteriorCodigo, plano.Codigo);

        assinaturas.AdicionarAuditoria(new AuditoriaAssinatura(
            tenantId, clienteId, evento,
            assinaturaId: assinatura.Id,
            usuarioId: currentUser.UserId,
            planoAnterior: planoAnteriorCodigo,
            novoPlano: plano.Codigo,
            valor: plano.ValorMensal,
            descricao: $"Cobrança PIX gerada (txid {txId}) — aguardando liquidação"));

        await assinaturas.SalvarAlteracoesAsync(cancellationToken);

        var beneficiario = pix.ObterBeneficiario();
        var dto = new PagamentoPlanoDto(
            pagamento.Id,
            assinatura.Id,
            clienteId,
            plano.Id,
            plano.Nome,
            pagamento.Valor,
            pagamento.Status.ToString(),
            pagamento.MetodoPagamento?.ToString(),
            pagamento.DataVencimento,
            pagamento.DataPagamento,
            pagamento.PixTxId,
            pagamento.PixCopiaCola,
            pagamento.PixQrCodeBase64,
            new BeneficiarioPixDto(
                beneficiario.Nome, beneficiario.Cnpj, beneficiario.Banco, beneficiario.ContaCorrente));

        return new ContratarPlanoResultadoDto(
            assinatura.Id, pagamento.Id, assinatura.Status.ToString(), dto);
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
                $"Não é possível contratar o plano {plano.Nome}: o cliente {string.Join("; ", motivos)}. " +
                "Adeque a utilização (inative imóveis/usuários excedentes) antes de alterar o plano.");
        }
    }

    private async Task<string?> ResolverCodigoPlanoAsync(Guid? planoId, CancellationToken ct)
    {
        if (planoId is null) return null;
        var p = await planos.ObterPorIdAsync(planoId.Value, ct);
        return p?.Codigo;
    }

    private static EventoAssinatura CompararPlanos(string anterior, string novo)
    {
        var ordem = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [PlanoCodigo.Trial] = 0,
            [PlanoCodigo.Basico] = 1,
            [PlanoCodigo.Intermediario] = 2,
            [PlanoCodigo.Avancado] = 3,
            [PlanoCodigo.Pro] = 4,
        };
        var a = ordem.GetValueOrDefault(anterior, 0);
        var b = ordem.GetValueOrDefault(novo, 0);
        return b >= a ? EventoAssinatura.Upgrade : EventoAssinatura.Downgrade;
    }

    /// <summary>txid Inter: 26–35 caracteres [a-zA-Z0-9].</summary>
    internal static string GerarTxId()
    {
        var raw = $"lucrare{Guid.NewGuid():N}";
        return raw.Length > 35 ? raw[..35] : raw.PadRight(26, '0');
    }
}
