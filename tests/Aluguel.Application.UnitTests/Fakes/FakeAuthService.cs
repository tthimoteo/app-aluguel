using Aluguel.Application.Abstractions;

namespace Aluguel.Application.UnitTests.Fakes;

/// <summary>Stub de <see cref="IAuthService"/> com vinculações em memória para testes de escopo.</summary>
public sealed class FakeAuthService : IAuthService
{
    public List<VinculoClienteDto> Vinculos { get; } = [];

    public Task<IReadOnlyList<VinculoClienteDto>> ListarVinculosAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VinculoClienteDto>>(Vinculos);

    public Task<ResultadoAuth> LoginAsync(string email, string senha, string? ip, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<ResultadoAuth> RefreshAsync(string refreshToken, string? ip, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task LogoutAsync(string refreshToken, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<ResultadoAuth> SelecionarClienteAsync(Guid userId, Guid clienteId, string refreshToken, string? ip,
        CancellationToken ct = default) =>
        throw new NotImplementedException();
}
