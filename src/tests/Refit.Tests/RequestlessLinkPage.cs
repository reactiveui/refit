// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;

namespace Refit.Tests;

/// <summary>A link page that is also an <see cref="IApiResponse"/> reporting no request message, as a mocked response would.</summary>
internal sealed class RequestlessLinkPage : IApiResponse
{
    /// <inheritdoc/>
    public HttpResponseHeaders? Headers => null;

    /// <inheritdoc/>
    public HttpContentHeaders? ContentHeaders => null;

    /// <inheritdoc/>
    public bool IsSuccessStatusCode => true;

    /// <inheritdoc/>
    public bool IsSuccessful => true;

    /// <inheritdoc/>
    public bool IsReceived => false;

    /// <inheritdoc/>
    public HttpStatusCode? StatusCode => HttpStatusCode.OK;

    /// <inheritdoc/>
    public string? ReasonPhrase => null;

    /// <inheritdoc/>
    public HttpRequestMessage? RequestMessage => null;

    /// <inheritdoc/>
    public Version? Version => null;

    /// <inheritdoc/>
    public ApiExceptionBase? Error => null;

    /// <summary>Gets the link to the following page, or <see langword="null"/> on the last page.</summary>
    internal Uri? Next { get; init; }

    /// <inheritdoc/>
    public bool HasRequestError([NotNullWhen(true)] out ApiRequestException? error)
    {
        error = null;
        return false;
    }

    /// <inheritdoc/>
    public bool HasResponseError([NotNullWhen(true)] out ApiException? error)
    {
        error = null;
        return false;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
