// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies the public constructors of <see cref="RestMethodParameterProperty"/>.</summary>
public sealed class RestMethodParameterPropertyTests
{
    /// <summary>Verifies the single-property constructor exposes a one-element chain whose final link is the property.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SinglePropertyConstructorExposesOneElementChain()
    {
        var property = typeof(Sample).GetProperty(nameof(Sample.Id))!;

        var parameterProperty = new RestMethodParameterProperty("id", property);

        await Assert.That(parameterProperty.Name).IsEqualTo("id");
        await Assert.That(parameterProperty.PropertyInfo).IsSameReferenceAs(property);
        await Assert.That(parameterProperty.PropertyChain).IsEquivalentTo([property]);
    }

    /// <summary>Verifies the chain constructor keeps the chain and surfaces its last link as the bound property.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ChainConstructorSurfacesLastLinkAsBoundProperty()
    {
        var outer = typeof(Sample).GetProperty(nameof(Sample.Inner))!;
        var inner = typeof(Inner).GetProperty(nameof(Inner.Id))!;

        var parameterProperty = new RestMethodParameterProperty("inner.id", [outer, inner]);

        await Assert.That(parameterProperty.Name).IsEqualTo("inner.id");
        await Assert.That(parameterProperty.PropertyInfo).IsSameReferenceAs(inner);
        await Assert.That(parameterProperty.PropertyChain).IsEquivalentTo([outer, inner]);
    }

    /// <summary>A model whose properties supply real metadata for the constructor fixtures.</summary>
    private sealed class Sample
    {
        /// <summary>The identifier value assigned to <see cref="Id"/>.</summary>
        private const int IdValue = 7;

        /// <summary>Gets or sets the identifier bound by a single-level chain.</summary>
        public int Id { get; set; } = IdValue;

        /// <summary>Gets or sets the nested object bound by a multi-level chain.</summary>
        public Inner? Inner { get; set; } = new();
    }

    /// <summary>A nested model exercised by the chain constructor.</summary>
    private sealed class Inner
    {
        /// <summary>The identifier value assigned to <see cref="Id"/>.</summary>
        private const int IdValue = 11;

        /// <summary>Gets or sets the nested identifier bound as the final chain link.</summary>
        public int Id { get; set; } = IdValue;
    }
}
