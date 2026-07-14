// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies request URI assembly under <see cref="UrlResolutionMode.Rfc3986"/>.</summary>
public sealed class Rfc3986RequestUriAssemblyTests
{
    /// <summary>A method without query parameters yields a bare relative path.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MethodWithoutQueryParametersYieldsBarePath()
    {
        var fixture = new RequestBuilderImplementation<IRfc3986RequestApi>(
            new RefitSettings { UrlResolution = UrlResolutionMode.Rfc3986 });
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IRfc3986RequestApi.GetWithoutQuery));
        var output = await factory([]);

        await Assert.That(output.RequestUri!.ToString()).IsEqualTo("http://api/items");
    }

    /// <summary>A method with a query parameter appends a built query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MethodWithQueryParametersAppendsQueryString()
    {
        var fixture = new RequestBuilderImplementation<IRfc3986RequestApi>(
            new RefitSettings { UrlResolution = UrlResolutionMode.Rfc3986 });
        var factory = fixture.BuildRequestFactoryForMethod(
            nameof(IRfc3986RequestApi.GetWithQuery));
        var output = await factory(["abc"]);

        await Assert.That(output.RequestUri!.ToString()).IsEqualTo("http://api/items?search=abc");
    }
}
