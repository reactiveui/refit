// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies a multi-segment route placeholder (<c>{param.inner.value}</c>) binds through a nested property
/// chain on a request object, exercising the reflection request builder's nested-property-chain resolver.</summary>
public sealed class NestedRoutePropertyBindingTests
{
    /// <summary>Verifies a nested property chain placeholder resolves to the nested property value in the request path.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NestedPropertyChainBindsToRoutePath()
    {
        var fixture = new RequestBuilderImplementation<INestedRouteApi>();
        var factory = fixture.BuildRequestFactoryForMethod(nameof(INestedRouteApi.GetByNestedValue));
        var request = new NestedRouteRequest { Inner = new NestedRouteInner { Value = "abc" } };

        var output = await factory([request]);

        await Assert.That(output.RequestUri!.ToString()).Contains("/items/abc");
    }
}
