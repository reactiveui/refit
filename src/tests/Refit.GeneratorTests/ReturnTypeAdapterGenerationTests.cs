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

    /// <summary>The deferred adapter invocation emitted for an adapter-backed return type.</summary>
    private const string AdaptCall = ".Adapt(";

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
        await Assert.That(result.GeneratedSources[Hint]).Contains(AdaptCall);
    }

    /// <summary>Verifies an adapter whose wrapper reorders its type parameters generates inline, closing the adapter over
    /// the reordered arguments (a successful compile proves the mapping is correct).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReorderedAdapterBackedReturnTypeGeneratesInline()
    {
        const string Source =
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Paired<TFirst, TSecond> { }

            // The wrapper lists the adapter's parameters in reverse: Paired<T2, T1>. A Paired<string, int> return must
            // bind T2 = string and T1 = int, closing the adapter as SwapAdapter<int, string> with a result type of int.
            public sealed class SwapAdapter<T1, T2> : IReturnTypeAdapter<Paired<T2, T1>, T1>
            {
                public Paired<T2, T1> Adapt(Func<CancellationToken, Task<T1>> invoke) => new();
            }

            public interface IGeneratedClient
            {
                [Get("/pairs/{id}")]
                Paired<string, int> GetPair(int id);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(result.GeneratedSources[Hint]).DoesNotContain(ReflectiveFallback);
        await Assert.That(result.GeneratedSources[Hint]).Contains(AdaptCall);
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
        await Assert.That(result.GeneratedSources[Hint]).Contains(AdaptCall);
    }

    /// <summary>
    /// Verifies the adapter matcher rejects registered adapters that do not surface a method's return type: a generic
    /// adapter with a different type-argument count and a non-generic adapter for a different return type. The method has
    /// no matching adapter, so it falls back.
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

            public interface IGeneratedClient
            {
                [Get("/pair")]
                Pair<int, string> GetPair();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();

        // WrappedAdapter<T> mismatches Pair's two-argument arity and BoxedAdapter is a different type, so no adapter
        // surfaces Pair<int, string> and the method falls back.
        await Assert.That(result.GeneratedSources[Hint]).Contains(ReflectiveFallback);
    }
}
