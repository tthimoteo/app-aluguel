using Aluguel.Application.Usuarios;

namespace Aluguel.Application.Abstractions;

/// <summary>
/// Porta de acesso aos usuários do cliente (implementada na Infrastructure sobre o ASP.NET Identity).
/// A identidade (e-mail/senha/CPF) é única no tenant; a vinculação a cada cliente é <c>usuario_cliente</c> (CASO 1).
/// </summary>
public interface IUsuarioService
{
    Task<UsuarioDto?> ObterPorIdAsync(Guid id, Guid? clienteId, CancellationToken ct = default);

    Task<IReadOnlyList<UsuarioDto>> ListarAsync(Guid clienteId, string? termo,
        int skip, int take, CancellationToken ct = default);

    Task<int> ContarAsync(Guid clienteId, string? termo, CancellationToken ct = default);

    Task<bool> EmailEmUsoAsync(string email, Guid? ignorarId, CancellationToken ct = default);

    Task<bool> CpfEmUsoNoClienteAsync(Guid clienteId, string cpf, Guid? ignorarId, CancellationToken ct = default);

    /// <summary>Localiza a identidade pelo CPF no tenant. <paramref name="clienteId"/> indica se já está neste cliente.</summary>
    Task<UsuarioPorCpfDto?> ObterPorCpfAsync(string cpf, Guid clienteId, CancellationToken ct = default);

    /// <summary>True se o cliente ainda está abaixo do limite de usuários do seu plano (UC002).</summary>
    Task<bool> PodeAdicionarUsuarioAsync(Guid clienteId, CancellationToken ct = default);

    Task<UsuarioDto> CriarAsync(NovoUsuario dados, CancellationToken ct = default);

    Task<UsuarioDto?> AtualizarAsync(Guid id, Guid clienteId, AtualizacaoUsuario dados, CancellationToken ct = default);

    /// <summary>Desativa a vinculação com o cliente (Status = Inativo), preservando a identidade.</summary>
    Task<bool> RemoverAsync(Guid id, Guid clienteId, CancellationToken ct = default);
}
