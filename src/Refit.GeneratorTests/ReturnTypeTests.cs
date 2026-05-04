namespace Refit.GeneratorTests;

public class ReturnTypeTests
{
    [Test]
    public Task GenericTaskShouldWork()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get();
            """);
    }

    [Test]
    public Task ReturnNullableObject()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string?> Get();
            """);
    }

    [Test]
    public Task ReturnNullableValueType()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<int?> Get();
            """);
    }

    [Test]
    public Task VoidTaskShouldWork()
    {
        return Fixture.VerifyForBody(
            """
            [Post("/users")]
            Task Post();
            """);
    }

    [Test]
    public Task GenericValueTaskShouldWork()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            ValueTask<string> Get();
            """);
    }

    [Test]
    public Task ValueTaskApiResponseShouldWork()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            ValueTask<ApiResponse<string>> Get();
            """);
    }

    [Test]
    public Task GenericConstraintReturnTask()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get<T>() where T : class, IDisposable, new();
            """);
    }

    [Test]
    public Task GenericUnmanagedConstraintReturnTask()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get<T>() where T : unmanaged;
            """);
    }

    [Test]
    public Task GenericStructConstraintReturnTask()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get<T>() where T : struct
            """);
    }

    [Test]
    public Task ReturnIObservable()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            IObservable<HttpResponseMessage> GetUser(string user);
            """);
    }

    [Test]
    public Task ReturnUnsupportedType()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users/{user}")]
            string GetUser(string user);
            """);
    }
}
