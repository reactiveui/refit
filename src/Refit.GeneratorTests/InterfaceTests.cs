namespace Refit.GeneratorTests;

public class InterfaceTests
{
    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
    public Task InterfacesWithDifferentCasing()
    {
        return Fixture.VerifyForType(
            """
            public interface IApi
            {
                [Get("/users")]
                Task<string> Get();
            }

            public interface Iapi
            {
                [Get("/users")]
                Task<string> Get();
            }
            """);
    }

    [Test]
    public Task InterfacesWithDifferentSignature()
    {
        return Fixture.VerifyForType(
            """
            public interface IApi
            {
                [Get("/users")]
                Task<string> Get();
            }

            public interface IApi<T>
            {
                [Get("/users")]
                Task<string> Get();
            }
            """);
    }

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
    public Task NonRefitMethodShouldRaiseDiagnostic()
    {
        return Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get();

            void NonRefitMethod();
            """);
    }

    [Test]
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
