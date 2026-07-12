// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.GeneratorTests;

/// <summary>
/// Verifies that a method returning a type served by a registered <c>IReturnTypeAdapter</c> generates inline,
/// surfacing the declared return type through a deferred <c>Adapt</c> call over the source-generated send.
/// </summary>
public sealed class ReturnTypeAdapterGenerationTests
{
    /// <summary>The generated implementation source hint name.</summary>
    private const string Hint = "IGeneratedClient.g.cs";

    /// <summary>The reflective request-builder call emitted by fallback paths.</summary>
    private const string ReflectiveFallback = "BuildRestResultFuncForMethod";

    /// <summary>Verifies an adapter-backed return type generates a deferred inline <c>Adapt</c> call.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AdapterBackedReturnTypeGeneratesInline()
    {
        const string Source =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Deferred<T>
            {
                private readonly Func<CancellationToken, Task<T>> _invoke;
                public Deferred(Func<CancellationToken, Task<T>> invoke) => _invoke = invoke;
                public Task<T> InvokeAsync(CancellationToken token) => _invoke(token);
            }

            public sealed class DeferredAdapter<T> : IReturnTypeAdapter<Deferred<T>, T>
            {
                public Deferred<T> Adapt(Func<CancellationToken, Task<T>> invoke) => new(invoke);
            }

            public interface IGeneratedClient
            {
                [Get("/users/{id}")]
                Deferred<string> GetUser(int id);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(result.GeneratedSources[Hint]).DoesNotContain(ReflectiveFallback);
        await Assert.That(result.GeneratedSources[Hint]).Contains(".Adapt(");
    }

    /// <summary>Verifies a non-generic adapter surfaces its non-generic return type inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NonGenericAdapterBackedReturnTypeGeneratesInline()
    {
        const string Source =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Boxed
            {
                public int Value { get; init; }
            }

            public sealed class BoxedAdapter : IReturnTypeAdapter<Boxed, int>
            {
                public Boxed Adapt(Func<CancellationToken, Task<int>> invoke) => new();
            }

            public interface IGeneratedClient
            {
                [Get("/count")]
                Boxed GetCount();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(result.GeneratedSources[Hint]).DoesNotContain(ReflectiveFallback);
        await Assert.That(result.GeneratedSources[Hint]).Contains(".Adapt(");
    }

    /// <summary>
    /// Verifies the adapter matcher rejects registered adapters that do not surface a method's return type: a generic
    /// adapter with a different type-argument count, a non-generic adapter for a different return type, and a generic
    /// adapter whose wrapper transposes the type parameters. The method has no matching adapter, so it falls back.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReturnTypeMatchingRejectsNonSurfacingAdapters()
    {
        const string Source =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Wrapped<T> { }

            public sealed class WrappedAdapter<T> : IReturnTypeAdapter<Wrapped<T>, T>
            {
                public Wrapped<T> Adapt(Func<CancellationToken, Task<T>> invoke) => new();
            }

            public sealed class Boxed { }

            public sealed class BoxedAdapter : IReturnTypeAdapter<Boxed, int>
            {
                public Boxed Adapt(Func<CancellationToken, Task<int>> invoke) => new();
            }

            public sealed class Pair<TLeft, TRight> { }

            public sealed class SwapAdapter<TLeft, TRight> : IReturnTypeAdapter<Pair<TRight, TLeft>, TLeft>
            {
                public Pair<TRight, TLeft> Adapt(Func<CancellationToken, Task<TLeft>> invoke) => new();
            }

            public interface IGeneratedClient
            {
                [Get("/pair")]
                Pair<int, string> GetPair();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();

        // No adapter surfaces Pair<int, string> (the SwapAdapter transposes the parameters), so the method falls back.
        await Assert.That(result.GeneratedSources[Hint]).Contains(ReflectiveFallback);
    }
}
