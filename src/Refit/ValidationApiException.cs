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
        ex.Content = DeserializeProblemDetails(exception.Content!);
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
        ArgumentExceptionHelper.ThrowIfNull(exception);

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

    /// <summary>Deserializes RFC 7807 problem details without requiring public setters on extension data.</summary>
    /// <param name="content">The JSON problem details content.</param>
    /// <returns>The deserialized problem details.</returns>
    private static ProblemDetails DeserializeProblemDetails(string content)
    {
#if NET10_0_OR_GREATER
        var rootElement = JsonElement.Parse(content);
#else
        using var document = JsonDocument.Parse(content);
        var rootElement = document.RootElement;
#endif
        if (rootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Problem details JSON must be an object.");
        }

        var problemDetails = new ProblemDetails();
        foreach (var property in rootElement.EnumerateObject())
        {
            ReadProblemDetailsProperty(problemDetails, property);
        }

        return problemDetails;
    }

    /// <summary>Reads a single problem-details property.</summary>
    /// <param name="problemDetails">The problem details instance to populate.</param>
    /// <param name="property">The JSON property to read.</param>
    private static void ReadProblemDetailsProperty(
        ProblemDetails problemDetails,
        JsonProperty property)
    {
        if (IsJsonProperty(property, "type"))
        {
            problemDetails.Type = ReadString(property.Value);
            return;
        }

        if (IsJsonProperty(property, "title"))
        {
            problemDetails.Title = ReadString(property.Value);
            return;
        }

        if (IsJsonProperty(property, "status"))
        {
            problemDetails.Status = property.Value.GetInt32();
            return;
        }

        if (IsJsonProperty(property, "detail"))
        {
            problemDetails.Detail = ReadString(property.Value);
            return;
        }

        if (IsJsonProperty(property, "instance"))
        {
            problemDetails.Instance = ReadString(property.Value);
            return;
        }

        if (IsJsonProperty(property, "errors"))
        {
            ReadErrors(property.Value, problemDetails.Errors);
            return;
        }

        problemDetails.Extensions[property.Name] = ReadExtensionValue(property.Value);
    }

    /// <summary>Determines whether a JSON property has the given name.</summary>
    /// <param name="property">The JSON property.</param>
    /// <param name="name">The expected property name.</param>
    /// <returns><see langword="true"/> when the names match.</returns>
    private static bool IsJsonProperty(JsonProperty property, string name) =>
        string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase);

    /// <summary>Reads a nullable JSON string value.</summary>
    /// <param name="element">The JSON element.</param>
    /// <returns>The string value.</returns>
    private static string? ReadString(JsonElement element) =>
        element.ValueKind == JsonValueKind.Null
            ? null
            : element.GetString();

    /// <summary>Reads validation errors from a JSON object.</summary>
    /// <param name="element">The JSON element.</param>
    /// <param name="errors">The error dictionary to populate.</param>
    private static void ReadErrors(
        JsonElement element,
        Dictionary<string, string[]> errors)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in element.EnumerateObject())
        {
            errors[property.Name] = ReadErrorMessages(property.Value);
        }
    }

    /// <summary>Reads error messages from a JSON value.</summary>
    /// <param name="element">The JSON element.</param>
    /// <returns>The error messages.</returns>
    private static string[] ReadErrorMessages(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            var messages = new List<string>();
            foreach (var item in element.EnumerateArray())
            {
                messages.Add(ReadErrorMessage(item));
            }

            return [.. messages];
        }

        return element.ValueKind == JsonValueKind.Null
            ? []
            : [ReadErrorMessage(element)];
    }

    /// <summary>Reads a single error message.</summary>
    /// <param name="element">The JSON element.</param>
    /// <returns>The error message.</returns>
    private static string ReadErrorMessage(JsonElement element) =>
        element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? string.Empty
            : element.GetRawText();

    /// <summary>Reads extension data using the same inferred primitives as the System.Text.Json converter.</summary>
    /// <param name="element">The JSON element.</param>
    /// <returns>The extension value.</returns>
    private static object ReadExtensionValue(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.String when element.TryGetDateTime(out var dateTime) => dateTime,
            JsonValueKind.String => element.GetString() ?? string.Empty,
            _ => element.Clone()
        };
}
