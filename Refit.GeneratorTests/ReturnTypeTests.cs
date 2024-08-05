namespace Refit.GeneratorTests;

[UsesVerify]
public class ReturnTypeTests
{
    [Fact]
    public Task GenericTaskShouldWork()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get();
            """);
    }

    [Fact]
    public Task VoidTaskShouldWork()
    {
        return Fixture.VerifyForBody(
            """
            [Post("/users")]
            Task Post();
            """);
    }
}
