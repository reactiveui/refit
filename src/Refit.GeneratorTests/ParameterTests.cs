namespace Refit.GeneratorTests;

public class ParameterTests
{
    [Test]
    public Task RouteParameter()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            Task<string> Get(string user);
            """);
    }

    [Test]
    public Task NullableRouteParameter()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            Task<string> Get(string? user);
            """);
    }

    [Test]
    public Task ValueTypeRouteParameter()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            Task<string> Get(int user);
            """);
    }

    [Test]
    public Task NullableValueTypeRouteParameter()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            Task<string> Get(int? user);
            """);
    }
}
