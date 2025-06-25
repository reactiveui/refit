namespace Refit.GeneratorTests;

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
    public Task ReturnNullableObject()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string?> Get();
            """);
    }

    [Fact]
    public Task ReturnNullableValueType()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<int?> Get();
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

    [Fact]
    public Task GenericConstraintReturnTask()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get<T>() where T : class, IDisposable, new();
            """);
    }

    [Fact]
    public Task GenericUnmanagedConstraintReturnTask()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get<T>() where T : unmanaged;
            """);
    }

    [Fact]
    public Task GenericStructConstraintReturnTask()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get<T>() where T : struct
            """);
    }

    [Fact]
    public Task ReturnIObservable()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            IObservable<HttpResponseMessage> GetUser(string user);
            """);
    }

    [Fact]
    public Task ReturnUnsupportedType()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            string GetUser(string user);
            """);
    }
}
