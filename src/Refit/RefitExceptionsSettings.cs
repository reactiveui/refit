// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Configures exceptions for a Refit client.</summary>
[System.Diagnostics.DebuggerDisplay("RefitExceptionsSettings")]
public sealed class RefitExceptionsSettings
{
    /// <summary>Initializes a new instance of the <see cref="RefitExceptionsSettings"/> class.</summary>
    /// <param name="settings">The settings used when constructing exceptions.</param>
    internal RefitExceptionsSettings(RefitSettings settings)
    {
        ExceptionFactory = new DefaultApiExceptionFactory(settings).CreateAsync;
        TransportExceptionFactory = settings.DefaultTransportExceptionFactory();
    }

    /// <summary>Gets or sets a function to provide <see cref="Exception"/> based on <see cref="HttpResponseMessage"/>. If function returns null - no exception is thrown.</summary>
    public Func<HttpResponseMessage, ValueTask<Exception?>> ExceptionFactory { get; set; }

    /// <summary>
    /// Gets or sets a function to provide <see cref="Exception"/> when deserialization exception is encountered.
    /// If function returns null - no exception is thrown.
    /// </summary>
    public Func<HttpResponseMessage, Exception, ValueTask<Exception?>>? DeserializationExceptionFactory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the request body is captured as a string before sending so it can be
    /// read from <see cref="ApiExceptionBase.RequestContent"/> when a request fails (defaults to false).
    /// </summary>
    /// <remarks>
    /// By default <see cref="HttpClient"/> disposes the request content once the request is sent, so the body cannot
    /// be read back from the exception. Enabling this buffers the body into memory before sending;
    /// avoid it for large or streamed uploads.
    /// <para>
    /// Security note: the captured body frequently contains credentials or PII (for example login, token-refresh, or
    /// payment payloads) and is exposed as a public property on the thrown exception. Use
    /// <see cref="ExceptionRedactor"/> to scrub it before the exception reaches a logging or telemetry pipeline that
    /// serializes exception objects.
    /// </para>
    /// </remarks>
    public bool CaptureRequestContent { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of characters of an error response body that are read into
    /// <see cref="ApiException.Content"/> (defaults to <see langword="null"/>, meaning unbounded).
    /// </summary>
    /// <remarks>
    /// Error responses are read with <see cref="HttpCompletionOption.ResponseHeadersRead"/>, so
    /// <see cref="HttpClient.MaxResponseContentBufferSize"/> does not apply. A hostile or oversized error body can
    /// therefore drive unbounded memory allocation while constructing the <see cref="ApiException"/>. Set a limit to
    /// bound that read; the body is truncated to the first <c>value</c> characters.
    /// </remarks>
    public int? MaxExceptionContentLength { get; set; }

    /// <summary>
    /// Gets or sets a hook invoked just before an <see cref="ApiExceptionBase"/> is returned, allowing sensitive data
    /// to be scrubbed before the exception propagates (defaults to <see langword="null"/>).
    /// </summary>
    /// <remarks>
    /// An <see cref="ApiException"/> retains the live request (including the <c>Authorization</c> header and any secret
    /// <c>[Header]</c> values), the optionally captured request body, and the raw response headers (including
    /// <c>Set-Cookie</c>) and body. Logging and telemetry libraries that serialize exceptions by walking properties
    /// will capture all of it. Use this hook to remove or mask those values, for example:
    /// <code>
    /// settings.ExceptionRedactor = ex =>
    /// {
    ///     ex.RequestMessage.Headers.Authorization = null;
    ///     if (ex is ApiException api) api.Content = null;
    /// };
    /// </code>
    /// </remarks>
    public Action<ApiExceptionBase>? ExceptionRedactor { get; set; }

    /// <summary>
    /// Gets or sets a factory invoked when <see cref="HttpClient.SendAsync(HttpRequestMessage)"/> throws a
    /// transport-level exception, giving callers full control over the exception that is ultimately surfaced.
    /// </summary>
    /// <remarks>
    /// The factory receives the <see cref="HttpRequestMessage"/>, the raw transport exception, and the
    /// <see cref="CancellationToken"/> that was passed to the call, and returns the exception to surface.
    /// <para>
    /// By default, an <see cref="OperationCanceledException"/> is returned unchanged when
    /// <see cref="CancellationToken.IsCancellationRequested"/> is <see langword="true"/> (so a cancelled
    /// <see cref="ApiResponse{T}"/> call throws instead of populating <c>Error</c>); every other exception
    /// is wrapped in an <see cref="ApiRequestException"/> that carries the full request context.
    /// </para>
    /// </remarks>
    public Func<HttpRequestMessage, Exception, CancellationToken, Exception> TransportExceptionFactory { get; set; }
}
