namespace Refit.GeneratorTests;

public class GeneratedTest
{
    [Fact]
    public Task ShouldEmitAllFiles()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get();
            """, false);
    }
}
