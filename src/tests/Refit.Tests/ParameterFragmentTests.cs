// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies the fragment-kind predicates of <see cref="ParameterFragment"/>.</summary>
public sealed class ParameterFragmentTests
{
    /// <summary>An arbitrary parameter index used when building fragments.</summary>
    private const int ArgumentIndex = 2;

    /// <summary>An arbitrary property index used when building object-property fragments.</summary>
    private const int PropertyIndex = 0;

    /// <summary>Verifies a dynamic route fragment is classified as a dynamic route and nothing else.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DynamicRouteFragmentIsClassifiedAsDynamicRoute()
    {
        var fragment = ParameterFragment.Dynamic(ArgumentIndex);

        await Assert.That(fragment.IsDynamicRoute).IsTrue();
        await Assert.That(fragment.IsConstant).IsFalse();
        await Assert.That(fragment.IsObjectProperty).IsFalse();
    }

    /// <summary>Verifies constant and object-property fragments are not classified as dynamic routes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ConstantAndObjectPropertyFragmentsAreNotDynamicRoutes()
    {
        await Assert.That(ParameterFragment.Constant("segment").IsDynamicRoute).IsFalse();
        await Assert.That(ParameterFragment.DynamicObject(ArgumentIndex, PropertyIndex).IsDynamicRoute).IsFalse();
    }

    /// <summary>Verifies a constant fragment (no argument index) is not classified as an object property, exercising
    /// the negative-argument-index short-circuit of the object-property predicate.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ConstantFragmentIsNotObjectProperty()
    {
        // ArgumentIndex is negative for a constant, so the first condition short-circuits false.
        await Assert.That(ParameterFragment.Constant("segment").IsObjectProperty).IsFalse();
    }

    /// <summary>Verifies an object-property fragment (both a parameter and a property index) is classified as an
    /// object property and nothing else.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ObjectPropertyFragmentIsClassifiedAsObjectProperty()
    {
        var fragment = ParameterFragment.DynamicObject(ArgumentIndex, PropertyIndex);

        // ArgumentIndex >= 0 and PropertyIndex >= 0, so IsObjectProperty is true.
        await Assert.That(fragment.IsObjectProperty).IsTrue();
        await Assert.That(fragment.IsConstant).IsFalse();
        await Assert.That(fragment.IsDynamicRoute).IsFalse();
    }
}
