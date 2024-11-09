namespace Refit.GeneratorTests;

public class MethodTests
{
    [Fact]
    public Task MethodsWithGenericConstraints()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get<T1, T2, T3, T4, T5>()
                where T1 : class
                where T2 : unmanaged
                where T3 : struct
                where T4 : notnull
                where T5 : class, IDisposable, new();

            void NonRefitMethod<T1, T2, T3, T4, T5>()
                where T1 : class
                where T2 : unmanaged
                where T3 : struct
                where T4 : notnull
                where T5 : class, IDisposable, new();
            """);
    }
}
