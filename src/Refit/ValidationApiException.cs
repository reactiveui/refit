// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Refit;

/// <summary>An ApiException that is raised according to RFC 7807, which contains problem details for validation exceptions.</summary>
[SuppressMessage(
    "Usage",
    "CA1032:Implement standard exception constructors",
    Justification = "This exception requires HTTP request/response context and cannot be constructed via the parameterless or message-only constructors.")]
[SuppressMessage(
    "Major Code Smell",
    "S4027:Exceptions should provide standard constructors",
    Justification = "This exception requires HTTP request/response context and cannot be constructed via the parameterless or message-only constructors.")]
public class ValidationApiException : ApiException
{
    /// <summary>Initializes a new instance of the <see cref="ValidationApiException"/> class.</summary>
    /// <param name="message">The exception message.</param>
#if NET8_0_OR_GREATER
#endif
    public ValidationApiException(string message)
        : base(
            message,
            new(),
            HttpMethod.Get,
            null,
            HttpStatusCode.InternalServerError,
            null,
            new HttpResponseMessage().Headers,
            new())
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ValidationApiException"/> class.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
#if NET8_0_OR_GREATER
#endif
    public ValidationApiException(string message, Exception innerException)
        : base(
            message,
            new(),
            HttpMethod.Get,
            null,
            HttpStatusCode.InternalServerError,
            null,
            new HttpResponseMessage().Headers,
            new(),
            innerException ?? throw new ArgumentNullException(nameof(innerException)))
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ValidationApiException"/> class from an existing ApiException.</summary>
    /// <param name="apiException">The originating API exception to wrap.</param>
    private ValidationApiException(ApiException apiException)
        : base(
            apiException.RequestMessage,
            apiException.HttpMethod,
            apiException.Content,
            apiException.StatusCode,
            apiException.ReasonPhrase,
            apiException.Headers,
            apiException.RefitSettings) =>

        // Carry over the content headers from the originating ApiException so callers
        // can inspect them on the validation exception too (#1945).
        ContentHeaders = apiException.ContentHeaders;

    /// <summary>Gets the problem details of the RFC 7807 validation exception.</summary>
    public new ProblemDetails? Content { get; private set; }

    /// <summary>Creates a new instance of a ValidationException from an existing ApiException.</summary>
    /// <param name="exception">An instance of an ApiException to use to build a ValidationException.</param>
    /// <returns>ValidationApiException.</returns>
    public static ValidationApiException Create(ApiException exception)
    {
        var ex = CreateCore(exception);
        ex.Content = JsonSerializer.Deserialize<ProblemDetails>(
            exception.Content!,
            ProblemDetailsJsonContext.Default.ProblemDetails);
        return ex;
    }

    /// <summary>
    /// Creates a new instance of a ValidationException from an existing ApiException,
    /// deserializing the problem details with the serializer configured in
    /// <see cref="RefitSettings"/>.
    /// </summary>
    /// <param name="exception">An instance of an ApiException to use to build a ValidationException.</param>
    /// <returns>ValidationApiException.</returns>
    internal static async Task<ValidationApiException> CreateAsync(ApiException exception)
    {
        var ex = CreateCore(exception);

        // Deserialize through the configured IHttpContentSerializer rather than a
        // hardcoded System.Text.Json instance, so problem details honor the user's
        // serializer (e.g. Newtonsoft) and its settings (#1197).
        using var content = new StringContent(
            exception.Content!,
            Encoding.UTF8,
            "application/problem+json");
        ex.Content = await exception
            .RefitSettings.ContentSerializer.FromHttpContentAsync<ProblemDetails>(content)
            .ConfigureAwait(false);

        return ex;
    }

    /// <summary>Validates the exception content and builds the base validation exception.</summary>
    /// <param name="exception">The API exception to convert.</param>
    /// <returns>A new validation exception wrapping the API exception.</returns>
    private static ValidationApiException CreateCore(ApiException exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(exception.Content);
#else
        if (string.IsNullOrWhiteSpace(exception.Content))
        {
            throw new ArgumentException(
                "Content must be an 'application/problem+json' compliant json string.");
        }
#endif

        return new(exception);
    }
}
