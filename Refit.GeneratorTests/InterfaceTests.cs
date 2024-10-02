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
        // this currently generates invalid code see issue #1801 for more information
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
}
