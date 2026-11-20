using Aluguel.Infrastructure.Storage;
using Xunit;

namespace Aluguel.Infrastructure.UnitTests;

public class StoragePathTests
{
    [Fact]
    public void ObjectKey_RemovePrefixoDoBucket()
    {
        Assert.Equal(
            "tenant/cliente/arquivo.pdf",
            StoragePath.ObjectKey("contracts", "contracts/tenant/cliente/arquivo.pdf"));
    }

    [Fact]
    public void LogicalPath_IncluiBucket()
    {
        Assert.Equal(
            "certificates/t/c/id.pfx",
            StoragePath.LogicalPath("certificates", "t/c/id.pfx"));
    }
}
