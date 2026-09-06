namespace Aluguel.Application.Autorizacao;

/// <summary>Nomes das roles do Identity (RBAC). Coincidem com <see cref="Domain.Usuarios.PerfilUsuario"/>.</summary>
public static class Perfis
{
    public const string Administrador = nameof(Administrador);
    public const string Gestor = nameof(Gestor);
    public const string Analista = nameof(Analista);

    public static readonly string[] Todos = [Administrador, Gestor, Analista];
}

/// <summary>Políticas de autorização de alto nível (agrupam roles por capacidade).</summary>
public static class Politicas
{
    /// <summary>Gestão de usuários/cadastros do cliente.</summary>
    public const string GerenciaUsuarios = nameof(GerenciaUsuarios);

    /// <summary>Gestão de cadastros operacionais (imóveis, inquilinos e contratos).</summary>
    public const string GerenciaCadastros = nameof(GerenciaCadastros);

    /// <summary>Emissão de NFS-e.</summary>
    public const string EmitirNfse = nameof(EmitirNfse);

    /// <summary>Cancelamento de NFS-e.</summary>
    public const string CancelarNfse = nameof(CancelarNfse);
}
