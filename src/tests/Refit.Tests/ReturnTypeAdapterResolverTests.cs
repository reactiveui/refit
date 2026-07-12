// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies the metadata-only matching rules of <see cref="ReturnTypeAdapterResolver"/> that the reflection
/// request builder relies on to surface custom return types through a registered <see cref="IReturnTypeAdapter{TReturn, TResult}"/>.</summary>
public sealed class ReturnTypeAdapterResolverTests
{
    /// <summary>Verifies a closed adapter surfaces its result type and ignores its non-adapter interfaces.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ClosedAdapterMatchingReturnTypeSurfacesResultType()
    {
        var matched = ReturnTypeAdapterResolver.TryResolveResultType(
            typeof(AdapterShape),
            [typeof(ClosedShapeAdapter)],
            out var resultType);

        await Assert.That(matched).IsTrue();
        await Assert.That(resultType).IsEqualTo(typeof(string));
    }

    /// <summary>Verifies a closed adapter whose declared return type differs is not matched.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ClosedAdapterWithDifferentReturnTypeIsNotMatched()
    {
        var matched = ReturnTypeAdapterResolver.TryResolveResultType(
            typeof(Wrapped<int>),
            [typeof(ClosedShapeAdapter)],
            out var resultType);

        await Assert.That(matched).IsFalse();
        await Assert.That(resultType).IsNull();
    }

    /// <summary>Verifies a null adapter entry is skipped without matching.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NullAdapterEntryIsSkipped()
    {
        var matched = ReturnTypeAdapterResolver.TryResolveResultType(
            typeof(AdapterShape),
            [null!],
            out var resultType);

        await Assert.That(matched).IsFalse();
        await Assert.That(resultType).IsNull();
    }

    /// <summary>Verifies an open generic adapter surfaces the wrapped result type positionally.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpenGenericAdapterSurfacesWrappedResultType()
    {
        var matched = ReturnTypeAdapterResolver.TryResolveResultType(
            typeof(Wrapped<AdapterUser>),
            [typeof(WrappedAdapter<>)],
            out var resultType);

        await Assert.That(matched).IsTrue();
        await Assert.That(resultType).IsEqualTo(typeof(AdapterUser));
    }

    /// <summary>Verifies an open generic adapter is not matched against a non-generic return type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpenGenericAdapterRejectsNonGenericReturnType()
    {
        var matched = ReturnTypeAdapterResolver.TryResolveResultType(
            typeof(string),
            [typeof(WrappedAdapter<>)],
            out _);

        await Assert.That(matched).IsFalse();
    }

    /// <summary>Verifies an open generic adapter is not matched when the return type arity differs.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpenGenericAdapterRejectsMismatchedArity()
    {
        var matched = ReturnTypeAdapterResolver.TryResolveResultType(
            typeof(Dictionary<string, int>),
            [typeof(WrappedAdapter<>)],
            out _);

        await Assert.That(matched).IsFalse();
    }

    /// <summary>Verifies non-adapter interfaces and a mismatched declared return shape are both skipped.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpenGenericAdapterSkipsNonAdapterInterfacesAndMismatchedShape()
    {
        var matched = ReturnTypeAdapterResolver.TryResolveResultType(
            typeof(List<int>),
            [typeof(MarkerAdapter<>)],
            out _);

        await Assert.That(matched).IsFalse();
    }

    /// <summary>Verifies a result type constructed over the adapter's type parameter is not matched during metadata
    /// resolution because closing it needs runtime instantiation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpenGenericAdapterWithConstructedResultTypeIsNotMatched()
    {
        var matched = ReturnTypeAdapterResolver.TryResolveResultType(
            typeof(Wrapped<int>),
            [typeof(ContainingResultAdapter<>)],
            out _);

        await Assert.That(matched).IsFalse();
    }

    /// <summary>Verifies a concrete, fully-closed result type on a generic adapter is surfaced verbatim.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpenGenericAdapterWithConcreteResultTypeSurfacesItVerbatim()
    {
        var matched = ReturnTypeAdapterResolver.TryResolveResultType(
            typeof(Wrapped<int>),
            [typeof(ConcreteResultAdapter<>)],
            out var resultType);

        await Assert.That(matched).IsTrue();
        await Assert.That(resultType).IsEqualTo(typeof(string));
    }

    /// <summary>Verifies an adapter whose declared return shape closes over a concrete argument is not matched.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpenGenericAdapterWithNonPositionalReturnShapeIsNotMatched()
    {
        var matched = ReturnTypeAdapterResolver.TryResolveResultType(
            typeof(Wrapped<int>),
            [typeof(PositionalMismatchAdapter<>)],
            out _);

        await Assert.That(matched).IsFalse();
    }

    /// <summary>Verifies the closed adapter type resolves to the registered adapter itself.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ResolveClosedAdapterTypeReturnsClosedAdapter()
    {
        var adapterType = ReturnTypeAdapterResolver.ResolveClosedAdapterType(
            typeof(AdapterShape),
            [typeof(ClosedShapeAdapter)]);

        await Assert.That(adapterType).IsEqualTo(typeof(ClosedShapeAdapter));
    }

    /// <summary>Verifies the closed adapter type is closed over the return type's arguments for a generic adapter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ResolveClosedAdapterTypeClosesGenericAdapter()
    {
        var adapterType = ReturnTypeAdapterResolver.ResolveClosedAdapterType(
            typeof(Wrapped<AdapterUser>),
            [typeof(WrappedAdapter<>)]);

        await Assert.That(adapterType).IsEqualTo(typeof(WrappedAdapter<AdapterUser>));
    }

    /// <summary>Verifies resolving a closed adapter type yields null when no adapter matches.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ResolveClosedAdapterTypeReturnsNullWhenNoAdapterMatches()
    {
        var adapterType = ReturnTypeAdapterResolver.ResolveClosedAdapterType(
            typeof(string),
            [typeof(ClosedShapeAdapter)]);

        await Assert.That(adapterType).IsNull();
    }

    /// <summary>A non-generic return shape surfaced by a closed adapter; no interface method returns it, so the
    /// generator never references the adapter.</summary>
    private sealed class AdapterShape
    {
        /// <summary>Gets the shape id.</summary>
        public int Id { get; init; }
    }

    /// <summary>A single-parameter generic return shape used by the generic adapters; no interface method returns it,
    /// so the generator never references the adapters.</summary>
    /// <typeparam name="T">The wrapped value type.</typeparam>
    private sealed class Wrapped<T>
    {
        /// <summary>Gets the wrapped value.</summary>
        public T? Value { get; init; }
    }

    /// <summary>A closed adapter surfacing <see cref="AdapterShape"/> as a string.</summary>
    private sealed class ClosedShapeAdapter : IReturnTypeAdapter<AdapterShape, string>
    {
        /// <inheritdoc/>
        public AdapterShape Adapt(Func<CancellationToken, Task<string>> invoke) => new();
    }

    /// <summary>An open generic adapter surfacing <c>Wrapped&lt;T&gt;</c> as <c>T</c>.</summary>
    /// <typeparam name="T">The wrapped result type.</typeparam>
    private sealed class WrappedAdapter<T> : IReturnTypeAdapter<Wrapped<T>, T>
    {
        /// <inheritdoc/>
        public Wrapped<T> Adapt(Func<CancellationToken, Task<T>> invoke) => new();
    }

    /// <summary>An open generic adapter that also implements a non-adapter interface and declares a return shape that
    /// does not match a plain generic return type.</summary>
    /// <typeparam name="T">The wrapped result type.</typeparam>
    private sealed class MarkerAdapter<T> : IReturnTypeAdapter<Wrapped<T>, T>, IDisposable
    {
        /// <inheritdoc/>
        public Wrapped<T> Adapt(Func<CancellationToken, Task<T>> invoke) => new();

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }

    /// <summary>An open generic adapter whose result type is a concrete, fully-closed type.</summary>
    /// <typeparam name="T">The wrapped return-shape type parameter.</typeparam>
    private sealed class ConcreteResultAdapter<T> : IReturnTypeAdapter<Wrapped<T>, string>
    {
        /// <inheritdoc/>
        public Wrapped<T> Adapt(Func<CancellationToken, Task<string>> invoke) => new();
    }

    /// <summary>An open generic adapter whose result type is itself constructed over the type parameter.</summary>
    /// <typeparam name="T">The wrapped result element type.</typeparam>
    private sealed class ContainingResultAdapter<T> : IReturnTypeAdapter<Wrapped<T>, List<T>>
    {
        /// <inheritdoc/>
        public Wrapped<T> Adapt(Func<CancellationToken, Task<List<T>>> invoke) => new();
    }

    /// <summary>An open generic adapter whose declared return shape closes over a concrete argument, not its parameter.</summary>
    /// <typeparam name="T">The unused wrapped result type parameter.</typeparam>
    private sealed class PositionalMismatchAdapter<T> : IReturnTypeAdapter<Wrapped<int>, T>
    {
        /// <inheritdoc/>
        public Wrapped<int> Adapt(Func<CancellationToken, Task<T>> invoke) => new();
    }
}
