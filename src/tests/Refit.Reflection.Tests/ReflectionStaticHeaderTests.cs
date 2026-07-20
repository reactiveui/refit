// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>Pins that class-level static headers are emitted unchanged, and that a dynamic header parameter overriding a
/// static header does not corrupt the shared static header set for subsequent calls.</summary>
public sealed class ReflectionStaticHeaderTests
{
    /// <summary>The static header emitted unchanged on every call.</summary>
    private const string StaticHeaderName = "X-Static";

    /// <summary>The value of the unchanged static header.</summary>
    private const string StaticHeaderValue = "static-value";

    /// <summary>The static header a dynamic parameter overrides.</summary>
    private const string OverrideHeaderName = "X-Override";

    /// <summary>The static default of the overridable header.</summary>
    private const string OverrideDefault = "original";

    /// <summary>Verifies a static-header-only method emits the same static headers on repeated calls.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StaticHeadersEmittedIdenticallyAcrossCalls()
    {
        // One builder (so both calls share the cached method metadata and its static header set) with a fresh
        // per-call request factory (each carries its own message handler).
        var builder = new RequestBuilderImplementation<IReflectionStaticHeaderApi>();

        var first = await builder.BuildRequestFactoryForMethod(nameof(IReflectionStaticHeaderApi.StaticOnly))([]);
        var second = await builder.BuildRequestFactoryForMethod(nameof(IReflectionStaticHeaderApi.StaticOnly))([]);

        await Assert.That(first.Headers.GetValues(StaticHeaderName).Single()).IsEqualTo(StaticHeaderValue);
        await Assert.That(first.Headers.GetValues(OverrideHeaderName).Single()).IsEqualTo(OverrideDefault);
        await Assert.That(second.Headers.GetValues(StaticHeaderName).Single()).IsEqualTo(StaticHeaderValue);
        await Assert.That(second.Headers.GetValues(OverrideHeaderName).Single()).IsEqualTo(OverrideDefault);
    }

    /// <summary>Verifies a dynamic header parameter overrides its static counterpart for that call without leaking the
    /// override into a later call that relies on the static default.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DynamicHeaderOverrideDoesNotLeakIntoStaticHeaders()
    {
        var builder = new RequestBuilderImplementation<IReflectionStaticHeaderApi>();

        var overridden = await builder
            .BuildRequestFactoryForMethod(nameof(IReflectionStaticHeaderApi.WithDynamic))(["call-specific"]);

        await Assert.That(overridden.Headers.GetValues(StaticHeaderName).Single()).IsEqualTo(StaticHeaderValue);
        await Assert.That(overridden.Headers.GetValues(OverrideHeaderName).Single()).IsEqualTo("call-specific");

        // A later static-only call must still see the original static default, proving the override never mutated the
        // shared static header set.
        var afterOverride = await builder
            .BuildRequestFactoryForMethod(nameof(IReflectionStaticHeaderApi.StaticOnly))([]);

        await Assert.That(afterOverride.Headers.GetValues(OverrideHeaderName).Single()).IsEqualTo(OverrideDefault);
    }
}
