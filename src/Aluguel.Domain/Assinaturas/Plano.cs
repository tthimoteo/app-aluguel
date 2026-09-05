using Aluguel.Domain.Common;

namespace Aluguel.Domain.Assinaturas;

/// <summary>Códigos dos planos comerciais (especificação §3).</summary>
public static class PlanoCodigo
{
    public const string Trial = "TRIAL";
    public const string Basico = "BASICO";
    public const string Intermediario = "INTERMEDIARIO";
    public const string Avancado = "AVANCADO";
    public const string Pro = "PRO";
}

/// <summary>Plano de assinatura do SaaS. Limites nulos significam "ilimitado/sob consulta" (Pro).</summary>
public class Plano : Entity
{
    public string Codigo { get; private set; } = default!;
    public string Nome { get; private set; } = default!;
    public int? MaxImoveis { get; private set; }
    public int? MaxUsuarios { get; private set; }
    public bool PermiteNfse { get; private set; }
    public int TrialDias { get; private set; }
    public decimal? ValorMensal { get; private set; }
    public bool Ativo { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Plano() { }

    public Plano(Guid id, string codigo, string nome, int? maxImoveis, int? maxUsuarios,
        bool permiteNfse, int trialDias, decimal? valorMensal)
    {
        Id = id;
        Codigo = codigo;
        Nome = nome;
        MaxImoveis = maxImoveis;
        MaxUsuarios = maxUsuarios;
        PermiteNfse = permiteNfse;
        TrialDias = trialDias;
        ValorMensal = valorMensal;
    }

    /// <summary>Indica se o cliente pode cadastrar mais um imóvel dado o total atual.</summary>
    public bool PermiteMaisImoveis(int totalAtual) => MaxImoveis is null || totalAtual < MaxImoveis;

    /// <summary>Indica se o cliente pode cadastrar mais um usuário dado o total atual.</summary>
    public bool PermiteMaisUsuarios(int totalAtual) => MaxUsuarios is null || totalAtual < MaxUsuarios;
}
