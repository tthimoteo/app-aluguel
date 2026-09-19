using Aluguel.Domain.Common;

namespace Aluguel.Domain.Usuarios;

/// <summary>
/// Vinculação de um usuário (identidade Unique por CPF) a um cliente, com perfil e status próprios (§6).
/// O mesmo CPF pode atuar em vários clientes; não pode haver duas vinculações ativas no mesmo cliente (CASO 1).
/// </summary>
public class UsuarioCliente : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid ClienteId { get; private set; }
    public PerfilUsuario Perfil { get; private set; }
    public StatusUsuario Status { get; private set; } = StatusUsuario.Ativo;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private UsuarioCliente() { }

    public UsuarioCliente(Guid tenantId, Guid usuarioId, Guid clienteId, PerfilUsuario perfil)
    {
        if (perfil is not (PerfilUsuario.Gestor or PerfilUsuario.Analista))
            throw new ArgumentException("Perfil da vinculação deve ser Gestor ou Analista.", nameof(perfil));

        TenantId = tenantId;
        UsuarioId = usuarioId;
        ClienteId = clienteId;
        Perfil = perfil;
    }

    public void Atualizar(PerfilUsuario perfil, StatusUsuario status)
    {
        if (perfil is not (PerfilUsuario.Gestor or PerfilUsuario.Analista))
            throw new ArgumentException("Perfil da vinculação deve ser Gestor ou Analista.", nameof(perfil));

        Perfil = perfil;
        Status = status;
        Touch();
    }

    /// <summary>Remove o usuário deste cliente (soft delete da vinculação), sem apagar a identidade.</summary>
    public void Inativar()
    {
        if (Status == StatusUsuario.Inativo)
            return;
        Status = StatusUsuario.Inativo;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
