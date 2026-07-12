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
}
