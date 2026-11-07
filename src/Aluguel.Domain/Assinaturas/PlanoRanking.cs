namespace Aluguel.Domain.Assinaturas;

/// <summary>Ordenação comercial dos planos para classificar upgrade/downgrade (§3).</summary>
public static class PlanoRanking
{
    public static int Ordem(string? codigo) => codigo?.ToUpperInvariant() switch
    {
        PlanoCodigo.Trial => 0,
        PlanoCodigo.Basico => 1,
        PlanoCodigo.Intermediario => 2,
        PlanoCodigo.Avancado => 3,
        PlanoCodigo.Pro => 4,
        _ => 0,
    };

    public static bool EhUpgrade(string? atual, string? novo) => Ordem(novo) > Ordem(atual);

    public static bool EhDowngrade(string? atual, string? novo) => Ordem(novo) < Ordem(atual);

    public static bool MesmoNivel(string? atual, string? novo) => Ordem(novo) == Ordem(atual);
}
