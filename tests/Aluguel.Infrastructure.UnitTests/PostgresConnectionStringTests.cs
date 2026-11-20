using Aluguel.Infrastructure.Persistence;
using Npgsql;
using Xunit;

namespace Aluguel.Infrastructure.UnitTests;

public class PostgresConnectionStringTests
{
    [Fact]
    public void Normalize_UriDoRender_ConverteParaNpgsqlComSsl()
    {
        var raw = "postgres://aluguel:p%40ss%2Fword@dpg-abc-a/aluguel";

        var normalized = PostgresConnectionString.Normalize(raw);

        var builder = new NpgsqlConnectionStringBuilder(normalized);
        Assert.Equal("dpg-abc-a", builder.Host);
        Assert.Equal(5432, builder.Port);
        Assert.Equal("aluguel", builder.Database);
        Assert.Equal("aluguel", builder.Username);
        Assert.Equal("p@ss/word", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
    }

    [Fact]
    public void Normalize_JaNoFormatoChaveValor_Mantem()
    {
        var raw = "Host=127.0.0.1;Port=5432;Database=aluguel;Username=aluguel;Password=aluguel_dev";

        var normalized = PostgresConnectionString.Normalize(raw);

        Assert.Equal(raw, normalized);
        _ = new NpgsqlConnectionStringBuilder(normalized);
    }

    [Fact]
    public void Normalize_Vazia_Lanca()
    {
        Assert.Throws<InvalidOperationException>(() => PostgresConnectionString.Normalize("  "));
    }
}
