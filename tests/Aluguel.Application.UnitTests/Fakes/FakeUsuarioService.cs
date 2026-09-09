using Aluguel.Application.Abstractions;
using Aluguel.Application.Usuarios;

namespace Aluguel.Application.UnitTests.Fakes;

/// <summary>Implementação em memória de <see cref="IUsuarioService"/> para testar handlers/validadores.</summary>
public sealed class FakeUsuarioService : IUsuarioService
{
    public List<UsuarioDto> Itens { get; } = [];
    public HashSet<string> EmailsEmUso { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<(Guid Cliente, string Cpf)> CpfsEmUso { get; } = [];
    public Dictionary<string, UsuarioPorCpfDto> PorCpf { get; } = new(StringComparer.Ordinal);
    public bool PodeAdicionar { get; set; } = true;

    public int CriouVezes { get; private set; }
    public NovoUsuario? UltimoCriado { get; private set; }
    public int RemoveuVezes { get; private set; }
    public AtualizacaoUsuario? UltimaAtualizacao { get; private set; }
    public Guid? UltimoClienteAtualizado { get; private set; }
    public Guid? UltimoClienteRemovido { get; private set; }

    public Task<UsuarioDto?> ObterPorIdAsync(Guid id, Guid? clienteId, CancellationToken ct = default) =>
        Task.FromResult(Itens.FirstOrDefault(u => u.Id == id && (clienteId is null || u.ClienteId == clienteId)));

    public Task<IReadOnlyList<UsuarioDto>> ListarAsync(Guid clienteId, string? termo,
        int skip, int take, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<UsuarioDto>>(
            Itens.Where(u => u.ClienteId == clienteId).Skip(skip).Take(take).ToList());

    public Task<int> ContarAsync(Guid clienteId, string? termo, CancellationToken ct = default) =>
        Task.FromResult(Itens.Count(u => u.ClienteId == clienteId));

    public Task<bool> EmailEmUsoAsync(string email, Guid? ignorarId, CancellationToken ct = default) =>
        Task.FromResult(EmailsEmUso.Contains(email.Trim()));

    public Task<bool> CpfEmUsoNoClienteAsync(Guid clienteId, string cpf, Guid? ignorarId, CancellationToken ct = default) =>
        Task.FromResult(CpfsEmUso.Contains((clienteId, cpf)));

    public Task<UsuarioPorCpfDto?> ObterPorCpfAsync(string cpf, Guid clienteId, CancellationToken ct = default)
    {
        if (PorCpf.TryGetValue(cpf, out var dto))
            return Task.FromResult<UsuarioPorCpfDto?>(dto);
        if (CpfsEmUso.Contains((clienteId, cpf)))
            return Task.FromResult<UsuarioPorCpfDto?>(
                new UsuarioPorCpfDto(Guid.NewGuid(), "Existente", "existente@demo.local", null, cpf, true));
        return Task.FromResult<UsuarioPorCpfDto?>(null);
    }

    public Task<bool> PodeAdicionarUsuarioAsync(Guid clienteId, CancellationToken ct = default) =>
        Task.FromResult(PodeAdicionar);

    public Task<UsuarioDto> CriarAsync(NovoUsuario dados, CancellationToken ct = default)
    {
        CriouVezes++;
        UltimoCriado = dados;
        var dto = new UsuarioDto(Guid.NewGuid(), dados.TenantId, dados.ClienteId, dados.Nome, dados.Email,
            dados.Cpf, dados.Telefone, dados.Perfil.ToString(), "Ativo", null);
        Itens.Add(dto);
        return Task.FromResult(dto);
    }

    public Task<UsuarioDto?> AtualizarAsync(Guid id, Guid clienteId, AtualizacaoUsuario dados, CancellationToken ct = default)
    {
        var indice = Itens.FindIndex(u => u.Id == id && u.ClienteId == clienteId);
        if (indice < 0)
            return Task.FromResult<UsuarioDto?>(null);

        UltimaAtualizacao = dados;
        UltimoClienteAtualizado = clienteId;
        var atual = Itens[indice];
        var atualizado = atual with
        {
            Nome = dados.Nome,
            Telefone = dados.Telefone,
            Perfil = dados.Perfil.ToString(),
            Status = dados.Status.ToString(),
        };
        Itens[indice] = atualizado;
        return Task.FromResult<UsuarioDto?>(atualizado);
    }

    public Task<bool> RemoverAsync(Guid id, Guid clienteId, CancellationToken ct = default)
    {
        RemoveuVezes++;
        UltimoClienteRemovido = clienteId;
        return Task.FromResult(Itens.Any(u => u.Id == id && u.ClienteId == clienteId));
    }
}

public sealed class FakeCurrentUser(Guid? userId, Guid? clienteId, bool ehAdministrador) : ICurrentUser
{
    public Guid? UserId { get; } = userId;
    public Guid? ClienteId { get; } = clienteId;
    public bool EhAdministrador { get; } = ehAdministrador;
}
