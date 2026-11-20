namespace Aluguel.Infrastructure.Persistence;

/// <summary>
/// Normaliza connection strings do Postgres para o formato Npgsql (chave=valor).
/// O Render Blueprint injeta URI <c>postgres://</c> / <c>postgresql://</c>, que o
/// <see cref="System.Data.Common.DbConnectionStringBuilder"/> rejeita com
/// "Format of the initialization string does not conform to specification starting at index 0".
/// </summary>
public static class PostgresConnectionString
{
    public static string Normalize(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:Postgres não configurada.");

        var raw = connectionString.Trim().Trim('"');

        if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return raw;

        // Uri precisa de postgres → http para parsear userinfo/host/path.
        var uri = new Uri(raw.Replace("postgresql://", "http://", StringComparison.OrdinalIgnoreCase)
            .Replace("postgres://", "http://", StringComparison.OrdinalIgnoreCase));

        var user = "";
        var password = "";
        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':', 2);
            user = Uri.UnescapeDataString(parts[0]);
            if (parts.Length > 1)
                password = Uri.UnescapeDataString(parts[1]);
        }

        var database = uri.AbsolutePath.Trim('/');
        var port = uri.IsDefaultPort ? 5432 : uri.Port;

        // Render Postgres exige SSL na conexão externa/interna gerenciada.
        return
            $"Host={uri.Host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require";
    }
}
