using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Aluguel.Application.Abstractions;
using Aluguel.Application.Assinaturas;
using Aluguel.Application.Common.Validacoes;
using Aluguel.Application.Usuarios;
using Aluguel.Domain.Assinaturas;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Tenancy;
using Aluguel.Domain.Usuarios;
using MediatR;

namespace Aluguel.Application.CadastroPublico.RegistrarContaPublica;

/// <summary>
/// Self-service: cria tenant + cliente PF + gestor + assinatura (Trial ou PIX).
/// </summary>
public sealed record RegistrarContaPublicaCommand(
    string Nome,
    string Email,
    string Telefone,
    string Cpf,
    string Senha,
    Guid? PlanoId) : IRequest<RegistrarContaPublicaResultadoDto>;

public sealed record RegistrarContaPublicaResultadoDto(
    Guid ClienteId,
    Guid AssinaturaId,
    string StatusAssinatura,
    string Operacao,
    string Mensagem,
    Guid? PagamentoId,
    PagamentoPlanoDto? Pagamento,
    TokensAutenticacao Tokens);

public sealed class RegistrarContaPublicaCommandHandler(
    ITenantStore tenants,
    IClienteRepository clientes,
    IAssinaturaRepository assinaturas,
    IPlanoRepository planos,
    IUsuarioService usuarios,
    IInterPixProvider pix,
    IAuthService auth,
    TimeProvider clock)
    : IRequestHandler<RegistrarContaPublicaCommand, RegistrarContaPublicaResultadoDto>
{
    public async Task<RegistrarContaPublicaResultadoDto> Handle(
        RegistrarContaPublicaCommand request, CancellationToken cancellationToken)
    {
        var nome = request.Nome?.Trim() ?? "";
        var email = request.Email?.Trim() ?? "";
        var telefone = Documento.SomenteDigitos(request.Telefone) ?? "";
        var cpf = Documento.SomenteDigitos(request.Cpf) ?? "";
        var senha = request.Senha ?? "";

        if (nome.Length < 3)
            throw new InvalidOperationException("Informe o nome completo.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new InvalidOperationException("Informe um e-mail válido.");
        if (telefone.Length < 10)
            throw new InvalidOperationException("Informe um telefone válido com DDD.");
        if (!Documento.CpfValido(cpf))
            throw new InvalidOperationException("CPF inválido.");
        if (senha.Length < 10)
            throw new InvalidOperationException("A senha deve ter pelo menos 10 caracteres.");

        if (await usuarios.EmailEmUsoAsync(email, null, cancellationToken))
            throw new InvalidOperationException("Já existe uma conta com este e-mail. Faça login ou recupere o acesso.");
        if (await usuarios.CpfIdentidadeEmUsoAsync(cpf, cancellationToken))
            throw new InvalidOperationException("Já existe uma conta com este CPF.");
        if (await clientes.CpfEmUsoAsync(cpf, null, cancellationToken))
            throw new InvalidOperationException("Já existe um cliente cadastrado com este CPF.");

        var planoTrial = await planos.ObterPorCodigoAsync(PlanoCodigo.Trial, cancellationToken)
            ?? throw new InvalidOperationException("Plano Trial não está disponível. Contate o suporte.");

        Plano? planoDestino = null;
        var modoTrial = request.PlanoId is null || request.PlanoId == planoTrial.Id;
        if (!modoTrial)
        {
            planoDestino = await planos.ObterPorIdAsync(request.PlanoId!.Value, cancellationToken)
                ?? throw new InvalidOperationException("Plano informado não foi encontrado.");
            if (!planoDestino.Ativo)
                throw new InvalidOperationException("Plano inativo não pode ser contratado.");
            if (planoDestino.Codigo == PlanoCodigo.Trial)
                modoTrial = true;
            else if (planoDestino.ValorMensal is null)
                throw new InvalidOperationException(
                    "O plano Pro é sob consulta. Use «Solicitar Demonstração» para falar com a Lucrare.");
            else if (planoDestino.ValorMensal <= 0)
                throw new InvalidOperationException("Plano sem valor mensal não gera cobrança PIX.");
        }

        var subdominio = await GerarSubdominioUnicoAsync(email, cancellationToken);
        var tenant = new Tenant(nome, subdominio);
        tenants.Adicionar(tenant);
        await tenants.SalvarAlteracoesAsync(cancellationToken);

        var cliente = Cliente.CriarPessoaFisica(
            tenant.Id, planoTrial.Id, nome, cpf, null, telefone, email, null);
        clientes.Adicionar(cliente);
        await clientes.SalvarAlteracoesAsync(cancellationToken);

        var agora = clock.GetUtcNow();
        Assinatura assinatura;
        string operacao;
        string mensagem;
        Guid? pagamentoId = null;
        PagamentoPlanoDto? pagamentoDto = null;

        if (modoTrial || planoDestino is null)
        {
            assinatura = new Assinatura(tenant.Id, cliente.Id, planoTrial.Id, 0m, StatusAssinatura.Trial);
            assinaturas.Adicionar(assinatura);
            assinaturas.AdicionarAuditoria(new AuditoriaAssinatura(
                tenant.Id, cliente.Id, EventoAssinatura.InicioTrial,
                assinaturaId: assinatura.Id,
                novoPlano: PlanoCodigo.Trial,
                descricao: "Trial de 7 dias iniciado no cadastro público"));
            await assinaturas.SalvarAlteracoesAsync(cancellationToken);
            operacao = "Trial";
            mensagem = "Conta criada com 7 dias de avaliação. Complete o cadastro e explore a plataforma.";
        }
        else
        {
            assinatura = new Assinatura(
                tenant.Id, cliente.Id, planoDestino.Id, planoDestino.ValorMensal!.Value,
                StatusAssinatura.PendentePagamento);
            assinaturas.Adicionar(assinatura);

            var (pagamento, _) = await AssinaturaPixHelper.CriarCobrancaPixAsync(
                assinatura,
                planoDestino.Id,
                planoDestino.ValorMensal.Value,
                $"Assinatura Lucrare — plano {planoDestino.Nome} (cadastro)",
                assinaturaJaRastreada: false,
                assinaturas,
                pix,
                agora,
                cancellationToken);

            assinaturas.AdicionarAuditoria(new AuditoriaAssinatura(
                tenant.Id, cliente.Id, EventoAssinatura.Contratacao,
                assinaturaId: assinatura.Id,
                novoPlano: planoDestino.Codigo,
                valor: planoDestino.ValorMensal,
                descricao: $"Cadastro público: PIX gerado (txid {pagamento.PixTxId})"));
            await assinaturas.SalvarAlteracoesAsync(cancellationToken);

            pagamentoId = pagamento.Id;
            pagamentoDto = AssinaturaPixHelper.ParaDto(
                pagamento, cliente.Id, planoDestino.Nome, pix.ObterBeneficiario());
            operacao = "Contratacao";
            mensagem =
                $"Conta criada. Pague o PIX do plano {planoDestino.Nome} para ativar a assinatura.";
        }

        await usuarios.CriarAsync(
            new NovoUsuario(
                tenant.Id, cliente.Id, nome, email, cpf, telefone, PerfilUsuario.Gestor, senha),
            cancellationToken);

        var login = await auth.LoginAsync(email, senha, ip: null, cancellationToken);
        if (!login.Sucesso || login.Tokens is null)
            throw new InvalidOperationException(
                login.Erro ?? "Conta criada, mas não foi possível iniciar a sessão. Faça login manualmente.");

        return new RegistrarContaPublicaResultadoDto(
            cliente.Id,
            assinatura.Id,
            assinatura.Status.ToString(),
            operacao,
            mensagem,
            pagamentoId,
            pagamentoDto,
            login.Tokens);
    }

    private async Task<string> GerarSubdominioUnicoAsync(string email, CancellationToken ct)
    {
        var local = email.Split('@')[0];
        var slug = Slugify(local);
        if (slug.Length < 3)
            slug = "conta";
        if (slug.Length > 40)
            slug = slug[..40];

        for (var i = 0; i < 8; i++)
        {
            var sufixo = Guid.NewGuid().ToString("N")[..6];
            var candidato = $"{slug}-{sufixo}";
            if (!await tenants.SubdominioEmUsoAsync(candidato, ct))
                return candidato;
        }

        return $"c-{Guid.NewGuid():N}"[..32];
    }

    private static string Slugify(string valor)
    {
        var normalizado = valor.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalizado.Length);
        foreach (var c in normalizado)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
            else if (c is '-' or '_' or '.')
                sb.Append('-');
        }

        var limpo = Regex.Replace(sb.ToString(), "-{2,}", "-").Trim('-');
        return limpo;
    }
}
