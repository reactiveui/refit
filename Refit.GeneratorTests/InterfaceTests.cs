namespace Refit.GeneratorTests;

public class InterfaceTests
{
    [Fact]
    public Task ContainedInterfaceTest()
    {
        return Fixture.VerifyForType(
            """
            public class ContainerType
            {
                public interface IContainedInterface
                {
                    [Get("/users")]
                    Task<string> Get();
                }
            }
            """);
    }

    [Fact]
    public Task RefitInterfaceDerivedFromRefitBaseTest()
    {
        return Fixture.VerifyForType(
            """
            public interface IGeneratedInterface : IBaseInterface
            {
                [Get("/users")]
                Task<string> Get();
            }

            public interface IBaseInterface
            {
                [Get("/posts")]
                Task<string> GetPosts();
            }
            """);
    }

    [Fact]
    public Task RefitInterfaceDerivedFromBaseTest()
    {
        return Fixture.VerifyForType(
            """
            public interface IGeneratedInterface : IBaseInterface
            {
                [Get("/users")]
                Task<string> Get();
            }

            public interface IBaseInterface
            {
                void NonRefitMethod();
            }
            """);
    }

    [Fact]
    public Task InterfaceDerivedFromRefitBaseTest()
    {
        return Fixture.VerifyForType(
            """
            public interface IBaseInterface
            {
                [Get("/users")]
                Task<string> Get();
            }

            public interface IDerivedInterface : IBaseInterface { }
            """);
    }

    [Fact]
    public Task DefaultInterfaceMethod()
    {
        return Fixture.VerifyForType(
            """
            public interface IGeneratedInterface
            {
                [Get("/users")]
                Task<string> Get();

                void Default() {{ Console.WriteLine("Default"); }}
            }
            """);
    }

    [Fact]
    public Task DerivedDefaultInterfaceMethod()
    {
        return Fixture.VerifyForType(
            """
            public interface IGeneratedInterface : IBaseInterface
            {
                [Get("/users")]
                Task<string> Get();
            }

            public interface IBaseInterface
            {
                [Get("/posts")]
                Task<string> GetPosts();

                void Default() {{ Console.WriteLine("Default"); }}
            }
            """);
    }

    [Fact]
    public Task NestedNamespaceTest()
    {
        return Fixture.VerifyForDeclaration(
            """
            namespace Nested.RefitGeneratorTest;

            public interface IGeneratedInterface
            {
                [Get("/users")]
                Task<string> Get();
            }
            """);
    }

    [Fact]
    public Task GlobalNamespaceTest()
    {
        return Fixture.VerifyForDeclaration(
            """
            public interface IGeneratedInterface
            {
                [Get("/users")]
                Task<string> Get();
            }
            """);
    }

    [Fact]
    public Task DisposableTest()
    {
        return Fixture.VerifyForDeclaration(
            """
            public interface IGeneratedInterface : IDisposable
            {
                [Get("/users")]
                Task<string> Get();
            }
            """);
    }

    [Fact]
    public Task NonRefitMethodShouldRaiseDiagnostic()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get();

            void NonRefitMethod();
            """);
    }

    [Fact]
    public Task InterfaceWithGenericConstraint()
    {
        return Fixture.VerifyForDeclaration(
            """
            public interface IGeneratedInterface<T1, T2, T3, T4, T5>
                where T1 : class
                where T2 : unmanaged
                where T3 : struct
                where T4 : notnull
                where T5 : class, IDisposable, new()
            {
                [Get("/users")]
                Task<string> Get();
            }
            """);
    }
}
