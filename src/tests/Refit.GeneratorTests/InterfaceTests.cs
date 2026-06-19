// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Generator tests covering various interface shapes and inheritance scenarios.</summary>
public class InterfaceTests
{
    /// <summary>Verifies generation for an interface nested inside a container type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task ContainedInterfaceTest() =>
        Fixture.VerifyForType(
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

    /// <summary>Verifies generation for a Refit interface derived from a Refit base interface.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task RefitInterfaceDerivedFromRefitBaseTest() =>
        Fixture.VerifyForType(
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

    /// <summary>Verifies generation for a Refit interface derived from a non-Refit base interface.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task RefitInterfaceDerivedFromBaseTest() =>
        Fixture.VerifyForType(
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

    /// <summary>Verifies generation for an empty interface derived from a Refit base interface.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task InterfaceDerivedFromRefitBaseTest() =>
        Fixture.VerifyForType(
            """
            public interface IBaseInterface
            {
                [Get("/users")]
                Task<string> Get();
            }

            public interface IDerivedInterface : IBaseInterface { }
            """);

    /// <summary>Verifies generation for an interface containing a default interface method.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task DefaultInterfaceMethod() =>
        Fixture.VerifyForType(
            """
            public interface IGeneratedInterface
            {
                [Get("/users")]
                Task<string> Get();

                void Default() {{ Console.WriteLine("Default"); }}
            }
            """);

    /// <summary>Verifies generation for an interface inheriting a default interface method from a base.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task DerivedDefaultInterfaceMethod() =>
        Fixture.VerifyForType(
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

    /// <summary>Verifies generation for two interfaces whose names differ only by casing.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task InterfacesWithDifferentCasing() =>
        Fixture.VerifyForType(
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

    /// <summary>Verifies generation for interfaces sharing a name but differing by generic arity.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task InterfacesWithDifferentSignature() =>
        Fixture.VerifyForType(
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

    /// <summary>Verifies generation for an interface in a nested namespace.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task NestedNamespaceTest() =>
        Fixture.VerifyForDeclaration(
            """
            namespace Nested.RefitGeneratorTest;

            public interface IGeneratedInterface
            {
                [Get("/users")]
                Task<string> Get();
            }
            """);

    /// <summary>Verifies generation for an interface in the global namespace.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task GlobalNamespaceTest() =>
        Fixture.VerifyForDeclaration(
            """
            public interface IGeneratedInterface
            {
                [Get("/users")]
                Task<string> Get();
            }
            """);

    /// <summary>Verifies generation for an interface that implements IDisposable.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task DisposableTest() =>
        Fixture.VerifyForDeclaration(
            """
            public interface IGeneratedInterface : IDisposable
            {
                [Get("/users")]
                Task<string> Get();
            }
            """);

    /// <summary>Verifies that a non-Refit method on the interface raises a diagnostic.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task NonRefitMethodShouldRaiseDiagnostic() =>
        Fixture.VerifyForBody(
            """
            [Get("/users")]
            Task<string> Get();

            void NonRefitMethod();
            """);

    /// <summary>Verifies generation for a generic interface with a variety of type constraints.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task InterfaceWithGenericConstraint() =>
        Fixture.VerifyForDeclaration(
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
