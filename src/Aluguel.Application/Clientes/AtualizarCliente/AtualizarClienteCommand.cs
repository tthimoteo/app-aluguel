using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Common;
using MediatR;

namespace Aluguel.Application.Clientes.AtualizarCliente;

/// <summary>Atualiza dados cadastrais. Tipo de pessoa e CPF/CNPJ são imutáveis.</summary>
public sealed record AtualizarClienteCommand(
    Guid Id,
    Guid? PlanoId,
    string? Nome,
    DateOnly? DataNascimento,
    string? RazaoSocial,
    string? NomeFantasia,
    string? InscricaoMunicipal,
    string? CnaePrincipal,
    string? CodigoServico,
    string? CodigoIndicadorOperacao,
    string? ClassificacaoTributaria,
    string? RegimeTributario,
    string? Telefone,
    string? Email,
    EnderecoInput? Endereco,
    StatusCliente? Status = null) : IRequest<ClienteDto?>;

public sealed class AtualizarClienteCommandHandler(
    IClienteRepository repositorio,
    IPlanoRepository planos,
    IImovelRepository imoveis,
    IUsuarioService usuarios,
    ICurrentUser currentUser)
    : IRequestHandler<AtualizarClienteCommand, ClienteDto?>
{
    public async Task<ClienteDto?> Handle(AtualizarClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        if (cliente is null || !EscopoCliente.PodeAcessar(currentUser, cliente.Id))
            return null;

        var endereco = request.Endereco?.ParaValueObject();

        if (cliente.TipoPessoa == TipoPessoa.PF)
            cliente.AtualizarDadosPessoaFisica(request.Nome!, request.DataNascimento,
                request.Telefone, request.Email, endereco);
        else
            cliente.AtualizarDadosPessoaJuridica(request.RazaoSocial!, request.NomeFantasia,
                request.InscricaoMunicipal, request.CnaePrincipal, request.Telefone, request.Email, endereco);

        RegimeTributario? regime = null;
        if (!string.IsNullOrWhiteSpace(request.RegimeTributario)
            && Enum.TryParse<RegimeTributario>(request.RegimeTributario, ignoreCase: true, out var r))
        {
            regime = r;
        }

        // CNAE/serviço/regime: PF também precisa quando o plano permite NFS-e.
        // Em PJ, CnaePrincipal já pode ter sido setado em AtualizarDadosPessoaJuridica — reaplicamos de forma unificada.
        cliente.DefinirDadosFiscaisNfse(
            request.CnaePrincipal,
            request.CodigoServico,
            regime,
            request.CodigoIndicadorOperacao,
            request.ClassificacaoTributaria);

        if (request.PlanoId is not null && request.PlanoId != cliente.PlanoId)
            await ValidarEAplicarPlanoAsync(cliente, request.PlanoId.Value, cancellationToken);

        if (request.Status is { } status)
            cliente.DefinirStatus(status);

        await repositorio.SalvarAlteracoesAsync(cancellationToken);
        return cliente.ParaDto();
    }

    /// <summary>Downgrade/upgrade: bloqueia se a utilização atual exceder os limites do novo plano (CASO 2).</summary>
    private async Task ValidarEAplicarPlanoAsync(Cliente cliente, Guid novoPlanoId, CancellationToken ct)
    {
        var plano = await planos.ObterPorIdAsync(novoPlanoId, ct)
            ?? throw new InvalidOperationException("Plano informado não foi encontrado.");

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
                $"Não é possível aplicar o plano {plano.Nome}: o cliente {string.Join("; ", motivos)}. " +
                "Adeque a utilização (inative imóveis/usuários excedentes) antes de alterar o plano.");
        }

        cliente.DefinirPlano(novoPlanoId);
    }
}
