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
    public Task DerivedRefitInterfaceTest()
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
    public Task DerivedNonRefitInterfaceTest()
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
}
