// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net.Http.Headers;

using Microsoft.Extensions.DependencyInjection;

namespace Refit;

/// <summary>Resolves each request's authorization token from a fresh per-request dependency-injection scope.</summary>
/// <remarks>
/// <see cref="IHttpClientFactory"/> pools message handlers for their configured lifetime, so a scoped service captured
/// directly by a handler would bleed across requests. Creating a scope here keeps the token provider correctly scoped
/// to each request without taking any ASP.NET Core dependency. Ambient <c>AsyncLocal</c>-based state (such as a
/// singleton <c>IHttpContextAccessor</c> registered by the host) still flows into the fresh scope, so the common
/// ASP.NET case works; a provider that reads the request message directly works too.
/// </remarks>
internal sealed class ScopedAuthorizationHeaderHandler : DelegatingHandler
{
    /// <summary>The default authorization scheme applied when the request carries no scheme of its own.</summary>
    private const string DefaultScheme = "Bearer";

    /// <summary>The root service provider used to create a fresh scope for every request.</summary>
    private readonly IServiceProvider _rootServiceProvider;

    /// <summary>The delegate that resolves the token from a per-request service provider.</summary>
    private readonly Func<IServiceProvider, HttpRequestMessage, CancellationToken, ValueTask<string>> _getToken;

    /// <summary>Initializes a new instance of the <see cref="ScopedAuthorizationHeaderHandler"/> class.</summary>
    /// <param name="rootServiceProvider">The root service provider used to create a per-request scope.</param>
    /// <param name="getToken">The delegate that resolves the token from a per-request service provider.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rootServiceProvider"/> or <paramref name="getToken"/> is null.</exception>
    public ScopedAuthorizationHeaderHandler(
        IServiceProvider rootServiceProvider,
        Func<IServiceProvider, HttpRequestMessage, CancellationToken, ValueTask<string>> getToken)
    {
        ArgumentExceptionHelper.ThrowIfNull(rootServiceProvider);
        ArgumentExceptionHelper.ThrowIfNull(getToken);
        _rootServiceProvider = rootServiceProvider;
        _getToken = getToken;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var scheme = request.Headers.Authorization?.Scheme ?? DefaultScheme;

        var scope = _rootServiceProvider.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var token = await _getToken(scope.ServiceProvider, request, cancellationToken).ConfigureAwait(false);

            // An empty token means "no credentials for this request": drop the header rather than
            // sending a blank `Authorization: <scheme>` value (#1688).
            request.Headers.Authorization = string.IsNullOrWhiteSpace(token)
                ? null
                : new AuthenticationHeaderValue(scheme, token);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
