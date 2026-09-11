namespace Kognia.ArchitectureTests;

public sealed class SmokeTests
{
    [Fact]
    public void DomainAssembly_ShouldBeLoadable()
    {
        var assembly = typeof(Kognia.Domain.AssemblyReference).Assembly;

        Assert.Equal("Kognia.Domain", assembly.GetName().Name);
    }
}
