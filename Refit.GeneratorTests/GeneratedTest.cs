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

    [Fact]
    public Task ShouldNotEmitFilesWhenNoRefitInterfaces()
    {
        // Refit should not generate any code when no valid Refit interfaces are present.
        return Fixture.VerifyForBody("", false);
    }
}
