namespace RefitGenerator.Tests;

[UsesVerify]
public class BaseGeneratorTests
{
    [Fact]
    public Task Test1()
    {
        var verify = VerifyXunit.Verifier.Verify("ranDriver");

        return verify.ToTask();
    }
}
