// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>Tests for problem details that the configured serializer cannot read.</summary>
public sealed partial class ApiExceptionTests
{
    /// <summary>A problem details body with a validation error and an extension member.</summary>
    private const string ProblemJson =
        "{\"title\":\"Invalid\",\"status\":400,\"errors\":{\"name\":[\"Name is required.\"]},\"traceId\":\"abc\"}";

    /// <summary>The validation message asserted by the fallback tests.</summary>
    private const string NameRequiredText = "Name is required.";

    /// <summary>Verifies a JSON context that does not describe <see cref="ProblemDetails"/> still yields a validation exception.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task CreateReadsProblemDetailsWhenContextDoesNotDescribeThem() =>
        AssertProblemDetailsRead(RefitSettings.ForJsonContext(InventoryJsonContext.Default));

    /// <summary>Verifies a JSON context that describes <see cref="ProblemDetails"/> still yields a validation exception.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task CreateReadsProblemDetailsWhenContextDescribesThem() =>
        AssertProblemDetailsRead(RefitSettings.ForJsonContext(ProblemDetailsJsonContext.Default));

    /// <summary>Verifies a serializer that does not support the type falls back to the metadata-free reader.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public Task CreateReadsProblemDetailsWhenSerializerThrows() =>
        AssertProblemDetailsRead(new(new ThrowingSerializer()));

    /// <summary>Verifies a body the serializer rejects is not read again by the fallback.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateKeepsPlainExceptionWhenSerializerRejectsTheBody()
    {
        const string ScalarErrorsJson = "{\"errors\":{\"name\":\"Name is required.\"}}";
        using var response = CreateErrorResponse(ScalarErrorsJson, ProblemJsonMediaType);

        var exception = await ApiException.Create(response.RequestMessage!, HttpMethod.Get, response, new());

        await Assert.That(exception is ValidationApiException).IsFalse();
        await Assert.That(exception.Content).IsEqualTo(ScalarErrorsJson);
    }

    /// <summary>Verifies a body the fallback cannot parse stays a plain exception with the raw content.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CreateKeepsPlainExceptionWhenFallbackCannotParseTheBody()
    {
        using var response = CreateErrorResponse("not json", ProblemJsonMediaType);

        var exception = await ApiException.Create(
            response.RequestMessage!,
            HttpMethod.Get,
            response,
            RefitSettings.ForJsonContext(InventoryJsonContext.Default));

        await Assert.That(exception is ValidationApiException).IsFalse();
        await Assert.That(exception.Content).IsEqualTo("not json");
    }

    /// <summary>Asserts a problem response becomes a populated validation exception under the given settings.</summary>
    /// <param name="settings">The settings used to create the exception.</param>
    /// <returns>A task representing the asynchronous assertion.</returns>
    private static async Task AssertProblemDetailsRead(RefitSettings settings)
    {
        using var response = CreateErrorResponse(ProblemJson, ProblemJsonMediaType);

        var exception = await ApiException.Create(response.RequestMessage!, HttpMethod.Get, response, settings);

        await Assert.That(exception).IsTypeOf<ValidationApiException>();
        var problem = ((ValidationApiException)exception).Content!;
        await Assert.That(problem.Title).IsEqualTo("Invalid");
        await Assert.That(problem.Status).IsEqualTo(ExpectedStatusCode);
        await Assert.That(problem.Errors["name"][0]).IsEqualTo(NameRequiredText);
        await Assert.That(problem.Extensions["traceId"].ToString()).IsEqualTo("abc");
    }

    /// <summary>A serializer that cannot read anything.</summary>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "Mirrors the explicit type parameters of the IHttpContentSerializer contract.")]
    private sealed class ThrowingSerializer : IHttpContentSerializer
    {
        /// <inheritdoc />
        public HttpContent ToHttpContent<T>(T item) => throw new NotSupportedException();

        /// <inheritdoc />
        public Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public string? GetFieldNameForProperty(System.Reflection.PropertyInfo propertyInfo) => null;
    }
}
