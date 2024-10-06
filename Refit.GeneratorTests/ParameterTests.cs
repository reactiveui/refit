namespace Refit.GeneratorTests;

public class ParameterTests
{
    [Fact]
    public Task RouteParameter()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            Task<string> Get(string user);
            """);
    }

    [Fact]
    public Task NullableRouteParameter()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            Task<string> Get(string? user);
            """);
    }

    [Fact]
    public Task ValueTypeRouteParameter()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            Task<string> Get(int user);
            """);
    }

    [Fact]
    public Task NullableValueTypeRouteParameter()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            Task<string> Get(int? user);
            """);
    }
}
