// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies <see cref="RestService.For{T}(HttpClient, RefitSettings?)"/> falls back to the type-keyed
/// generated settings factory when the strongly typed holder for the interface is unset.</summary>
public sealed class RestServiceGeneratedFactoryFallbackTests
{
    /// <summary>Verifies the type-keyed settings factory is used when the generic holder is null.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ForUsesTypeKeyedSettingsFactoryWhenGenericHolderIsUnset()
    {
        var stub = new RestServiceFallbackApiStub();
        RestService.RegisterGeneratedSettingsFactory<IRestServiceFallbackApi>((_, _) => stub);

        // Clear only the strongly typed holder so For<T> falls through to the type-keyed dictionary registered above.
        RestService.GeneratedSettingsFactory<IRestServiceFallbackApi>.Factory = null;

        using var client = new HttpClient { BaseAddress = new("http://api/") };
        var api = RestService.For<IRestServiceFallbackApi>(client, new RefitSettings());

        await Assert.That(api).IsSameReferenceAs(stub);
    }
}
