namespace RefitGenerator.Tests;

[UsesVerify]
public class RefitGeneratorTests : BaseTestClass
{
    [Fact]
    public async Task NoRefitInterfacesSmokeTest()
    {
        var input = await Fixture.GetFileFromRefitTest("IInterfaceWithoutRefit.cs");
        await Fixture.VerifyGenerator(input);
    }

    [Fact]
    public async Task FindInterfacesSmokeTest()
    {
        var input = await Fixture.GetFileFromRefitTest("GitHubApi.cs");
        await Fixture.VerifyGenerator(input);
    }

    [Fact]
    public async Task GenerateInterfaceStubsWithoutNamespaceSmokeTest()
    {
        var input = await Fixture.GetFileFromRefitTest("IServiceWithoutNamespace.cs");
        await Fixture.VerifyGenerator(input);
    }
}
