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

    /// <summary>Verifies a closed adapter that also implements a non-adapter interface skips that interface during
    /// matching: the non-adapter interface is evaluated and rejected, and the mismatched return type is not matched.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ClosedAdapterWithNonAdapterInterfaceSkipsNonAdapterInterface()
    {
        // The return type does not match the adapter's declared return type, so no interface causes an early match.
        // Every implemented interface (including the non-adapter IDisposable) is inspected and skipped.
        var matched = ReturnTypeAdapterResolver.TryResolveResultType(
            typeof(Wrapped<int>),
            [typeof(ClosedShapeAdapterWithMarker)],
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

    /// <summary>Verifies an open generic adapter whose wrapper reorders the adapter's type parameters is matched, closing
    /// the adapter over the reordered arguments and surfacing the correctly mapped result type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpenGenericAdapterSurfacesReorderedWrappedResultType()
    {
        // SwappedAdapter<T1, T2> : IReturnTypeAdapter<Paired<T2, T1>, T1>, so a Paired<AdapterUser, int> return binds
        // T2 = AdapterUser and T1 = int; the surfaced result type is T1 = int.
        var matched = ReturnTypeAdapterResolver.TryResolveResultType(
            typeof(Paired<AdapterUser, int>),
            [typeof(SwappedAdapter<,>)],
            out var resultType);

        await Assert.That(matched).IsTrue();
        await Assert.That(resultType).IsEqualTo(typeof(int));
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

    /// <summary>Verifies an open generic adapter whose declared return shape is non-generic is not matched, because a
    /// non-generic template return cannot bind the return type's arguments positionally.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpenGenericAdapterWithNonGenericTemplateReturnIsNotMatched()
    {
        // The return type is generic with matching arity, so mapping is attempted; the adapter's template return
        // (AdapterShape) is non-generic, so the argument mapping fails immediately.
        var matched = ReturnTypeAdapterResolver.TryResolveResultType(
            typeof(Wrapped<int>),
            [typeof(NonGenericTemplateReturnAdapter<>)],
            out var resultType);

        await Assert.That(matched).IsFalse();
        await Assert.That(resultType).IsNull();
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

    /// <summary>Verifies a generic adapter whose wrapper pins a concrete argument is rejected when the return type's
    /// argument at that position differs, so the concrete-versus-return comparison fails the whole bind.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpenGenericAdapterWithConcreteWrapperArgumentRejectsDifferingReturnArgument()
    {
        // PositionalMismatchAdapter<T> : IReturnTypeAdapter<Wrapped<int>, T>. Binding a Wrapped<string> return keeps the
        // arity match but the wrapper's concrete int argument cannot bind the return's string argument, so the map fails.
        var matched = ReturnTypeAdapterResolver.TryResolveResultType(
            typeof(Wrapped<string>),
            [typeof(PositionalMismatchAdapter<>)],
            out var resultType);

        await Assert.That(matched).IsFalse();
        await Assert.That(resultType).IsNull();
    }

    /// <summary>Verifies a generic adapter whose wrapper repeats a type parameter enforces a consistent binding: the same
    /// return argument at both positions is accepted, while a differing argument rejects the bind.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task OpenGenericAdapterWithRepeatedWrapperParameterRequiresConsistentBinding()
    {
        // RepeatedWrapperAdapter<TValue, TResult> : IReturnTypeAdapter<Paired<TValue, TValue>, TResult>. Both wrapper
        // positions bind the same parameter, so the second position re-checks the first binding for consistency. Because
        // TResult never appears in the wrapper, neither binding closes the adapter, so both resolve to no match. The value
        // lies in exercising the consistent (Paired<int, int>) and inconsistent (Paired<int, string>) outcomes of that re-check.
        var consistent = ReturnTypeAdapterResolver.TryResolveResultType(
            typeof(Paired<int, int>),
            [typeof(RepeatedWrapperAdapter<,>)],
            out _);

        var inconsistent = ReturnTypeAdapterResolver.TryResolveResultType(
            typeof(Paired<int, string>),
            [typeof(RepeatedWrapperAdapter<,>)],
            out _);

        await Assert.That(consistent).IsFalse();
        await Assert.That(inconsistent).IsFalse();
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

    /// <summary>A closed adapter surfacing <see cref="AdapterShape"/> as a string that also implements a non-adapter
    /// interface, so matching must skip the non-adapter interface.</summary>
    private sealed class ClosedShapeAdapterWithMarker : IReturnTypeAdapter<AdapterShape, string>, IDisposable
    {
        /// <inheritdoc/>
        public AdapterShape Adapt(Func<CancellationToken, Task<string>> invoke) => new();

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }

    /// <summary>An open generic adapter whose declared return shape is a non-generic type, so it cannot map a generic
    /// return type's arguments positionally.</summary>
    /// <typeparam name="T">The unused result type parameter.</typeparam>
    private sealed class NonGenericTemplateReturnAdapter<T> : IReturnTypeAdapter<AdapterShape, T>
    {
        /// <inheritdoc/>
        public AdapterShape Adapt(Func<CancellationToken, Task<T>> invoke) => new();
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

    /// <summary>A two-parameter generic return shape used by the reordered adapter.</summary>
    /// <typeparam name="TFirst">The first wrapped value type.</typeparam>
    /// <typeparam name="TSecond">The second wrapped value type.</typeparam>
    private sealed class Paired<TFirst, TSecond>
    {
        /// <summary>Gets the first wrapped value.</summary>
        public TFirst? First { get; init; }

        /// <summary>Gets the second wrapped value.</summary>
        public TSecond? Second { get; init; }
    }

    /// <summary>An open generic adapter whose wrapper reorders the adapter's type parameters.</summary>
    /// <typeparam name="T1">The result type parameter, appearing second in the wrapper.</typeparam>
    /// <typeparam name="T2">The type parameter appearing first in the wrapper.</typeparam>
    private sealed class SwappedAdapter<T1, T2> : IReturnTypeAdapter<Paired<T2, T1>, T1>
    {
        /// <inheritdoc/>
        public Paired<T2, T1> Adapt(Func<CancellationToken, Task<T1>> invoke) => new();
    }

    /// <summary>An open generic adapter whose wrapper binds a single type parameter to both of its positions, so matching
    /// re-checks the second position against the first binding for consistency.</summary>
    /// <typeparam name="TValue">The type parameter bound at both wrapper positions.</typeparam>
    /// <typeparam name="TResult">The result type parameter, absent from the wrapper.</typeparam>
    private sealed class RepeatedWrapperAdapter<TValue, TResult> : IReturnTypeAdapter<Paired<TValue, TValue>, TResult>
    {
        /// <inheritdoc/>
        public Paired<TValue, TValue> Adapt(Func<CancellationToken, Task<TResult>> invoke) => new();
    }
}
