using Aluguel.Application.Usuarios;

namespace Aluguel.Application.Abstractions;

/// <summary>
/// Porta de acesso aos usuários do cliente (implementada na Infrastructure sobre o ASP.NET Identity).
/// As leituras já são escopadas pelo tenant corrente; a unicidade de CPF é por cliente (CASO 1).
/// </summary>
public interface IUsuarioService
{
    Task<UsuarioDto?> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<UsuarioDto>> ListarAsync(Guid clienteId, string? termo,
        int skip, int take, CancellationToken ct = default);

    Task<int> ContarAsync(Guid clienteId, string? termo, CancellationToken ct = default);

    Task<bool> EmailEmUsoAsync(string email, Guid? ignorarId, CancellationToken ct = default);

    Task<bool> CpfEmUsoNoClienteAsync(Guid clienteId, string cpf, Guid? ignorarId, CancellationToken ct = default);

    /// <summary>True se o cliente ainda está abaixo do limite de usuários do seu plano (UC002).</summary>
    Task<bool> PodeAdicionarUsuarioAsync(Guid clienteId, CancellationToken ct = default);

    Task<UsuarioDto> CriarAsync(NovoUsuario dados, CancellationToken ct = default);

    Task<UsuarioDto?> AtualizarAsync(Guid id, AtualizacaoUsuario dados, CancellationToken ct = default);

    /// <summary>Desativação lógica (Status = Inativo) — preserva o histórico/auditoria do usuário.</summary>
    Task<bool> RemoverAsync(Guid id, CancellationToken ct = default);
}
