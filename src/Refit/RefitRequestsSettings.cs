// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Configures requests for a Refit client.</summary>
[System.Diagnostics.DebuggerDisplay("RefitRequestsSettings")]
public sealed class RefitRequestsSettings
{
    /// <summary>The request options initialized through grouped or flat settings.</summary>
    private Dictionary<string, object>? _requestOptions;

    /// <summary>Gets or sets the token getter for a declared Authorization header without a token.</summary>
    /// <remarks>
    /// Generated preparation fills only a missing token, including with a supplied HttpClient.
    /// A handler installed from settings also invokes the getter for explicit tokens. An empty token removes the header.
    /// </remarks>
    public Func<HttpRequestMessage, CancellationToken, ValueTask<string>>? AuthorizationHeaderValueGetter { get; set; }

    /// <summary>Gets or sets a custom inner HttpMessageHandler. Does not work if you supply an HttpClient instance.</summary>
    public Func<HttpMessageHandler>? HttpMessageHandlerFactory { get; set; }

    /// <summary>Gets the return-type adapters the opt-in reflection request builder uses to surface custom return
    /// shapes (for example <c>IObservable&lt;T&gt;</c> or <c>Result&lt;T&gt;</c>) from interface methods.</summary>
    /// <remarks>
    /// Each entry is a type implementing <see cref="IReturnTypeAdapter{TReturn, TResult}"/>: either a closed type,
    /// or an open generic definition whose single type parameter is the wrapped result type. The source generator
    /// does not consult this collection - it discovers adapters at compile time - so this only affects
    /// <see cref="RestService.For{T}(HttpClient, RefitSettings)"/> and other reflection-based builds.
    /// </remarks>
    public List<Type> ReturnTypeAdapters { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the current call's argument values are captured, in declared
    /// parameter order, into the <see cref="HttpRequestMessageOptions.MethodArguments"/> request option so a
    /// <see cref="DelegatingHandler"/> can inspect them (defaults to false).
    /// </summary>
    /// <remarks>
    /// When enabled, every call boxes its arguments into an <c>object?[]</c> that is retained on the
    /// <see cref="HttpRequestMessage"/> (and therefore on any resulting <see cref="ApiExceptionBase"/>) for the
    /// lifetime of the request. Leave it off unless a handler needs the values: it adds a per-call allocation, keeps
    /// otherwise-collectable arguments alive, and the captured values frequently contain credentials or PII. The array
    /// includes any <see cref="CancellationToken"/> so it aligns 1:1 with the reflected parameter list.
    /// </remarks>
    public bool CaptureMethodArguments { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether header values are validated against the framework's header parsers
    /// when they are applied to a request (defaults to <see langword="false"/>).
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, header values are added with <see cref="System.Net.Http.Headers.HttpHeaders.Add(string, string?)"/>,
    /// so a value the framework parser rejects (for example an invalid <c>User-Agent</c>, malformed date, or a
    /// disallowed character) throws <see cref="FormatException"/> while the request is being built. When
    /// <see langword="false"/> (the default) values are added verbatim with
    /// <see cref="System.Net.Http.Headers.HttpHeaders.TryAddWithoutValidation(string, string?)"/>, preserving Refit's
    /// long-standing behaviour of sending header values as supplied. Carriage-return and line-feed characters are
    /// stripped from header names and values in both modes to guard against header injection.
    /// </remarks>
    public bool ValidateHeaders { get; set; }

    /// <summary>Gets or initializes local values copied into every request's options on modern .NET, or properties on .NET Framework.</summary>
    public Dictionary<string, object>? HttpRequestMessageOptions
    {
        get => _requestOptions;
        init => _requestOptions = value;
    }

#if NET6_0_OR_GREATER
    /// <summary>Gets or sets the HTTP version requested by outgoing calls; defaults to HTTP/1.1.</summary>
    public Version Version { get; set; } = System.Net.HttpVersion.Version11;

#endif
#if NET6_0_OR_GREATER
    /// <summary>Gets or sets HTTP version negotiation; defaults to accepting the requested version or a lower version.</summary>
    public HttpVersionPolicy VersionPolicy { get; set; } = HttpVersionPolicy.RequestVersionOrLower;

#endif

    /// <summary>Initializes request options from the owning settings' compatibility property.</summary>
    /// <param name="options">The request options to store.</param>
    internal void InitializeRequestOptions(Dictionary<string, object>? options) => _requestOptions = options;
}
