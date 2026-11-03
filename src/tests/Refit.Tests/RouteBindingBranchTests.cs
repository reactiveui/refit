// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies route placeholder binding paths that ordinary interfaces rarely reach.</summary>
public sealed class RouteBindingBranchTests
{
    /// <summary>The interface whose methods exercise the route binding paths.</summary>
    public interface IRouteBindingApi
    {
        /// <summary>A round-trip placeholder with no matching parameter.</summary>
        /// <param name="other">An unrelated parameter.</param>
        /// <returns>A task that represents the request.</returns>
        [Get("/a/{**rest}")]
        Task UnmatchedRoundTrip(string other);

        /// <summary>Two placeholders bound to properties of one request object.</summary>
        /// <param name="request">The request object.</param>
        /// <returns>A task that represents the request.</returns>
        [Get("/a/{request.Id}/{request.Name}")]
        Task TwoProperties(RouteRequest request);
    }

    /// <summary>Verifies a round-trip placeholder with no matching parameter is left in the path when unmatched placeholders are allowed.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UnmatchedRoundTripPlaceholderIsLeftInPlace()
    {
        var input = typeof(IRouteBindingApi);
        var method = input.GetMethods().First(static x => x.Name == nameof(IRouteBindingApi.UnmatchedRoundTrip));

        var fixture = new RestMethodInfoInternal(input, method, new() { AllowUnmatchedRouteParameters = true });

        await Assert.That(fixture.ParameterMap.Count).IsEqualTo(0);
    }

    /// <summary>Verifies two placeholders bound to properties of one object share a single property lookup.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task TwoPropertyPlaceholdersBindToTheSameObject()
    {
        var input = typeof(IRouteBindingApi);
        var method = input.GetMethods().First(static x => x.Name == nameof(IRouteBindingApi.TwoProperties));

        var fixture = new RestMethodInfoInternal(input, method);

        await Assert.That(fixture.ParameterMap.Count).IsEqualTo(1);
    }

    /// <summary>The request object whose properties fill route placeholders.</summary>
    public sealed class RouteRequest
    {
        /// <summary>Gets or sets the identifier.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the name.</summary>
        public string? Name { get; set; }
    }
}
