namespace Aluguel.Infrastructure.Storage;

/// <summary>Configuração de storage (local ou Supabase). Seção <c>Storage</c>.</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary><c>Local</c> (dev) ou <c>Supabase</c> (produção).</summary>
    public string Provider { get; set; } = "Local";

    public string LocalRoot { get; set; } = "storage";

    /// <summary>URL do projeto, ex. https://xxxx.supabase.co</summary>
    public string? SupabaseUrl { get; set; }

    /// <summary>service_role (somente backend). Nunca anon key.</summary>
    public string? SupabaseServiceRoleKey { get; set; }

    public bool IsSupabase =>
        string.Equals(Provider, "Supabase", StringComparison.OrdinalIgnoreCase);
}
