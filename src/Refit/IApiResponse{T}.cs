// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace Refit;

/// <inheritdoc/>
/// <typeparam name="T">The deserialized response content type.</typeparam>
public interface IApiResponse<out T> : IApiResponse
{
    /// <summary>Gets the exception object in case of unsuccessful request or response.</summary>
    /// <remarks>
    /// The <see cref="IApiResponse.HasRequestError"/> and <see cref="IApiResponse.HasResponseError"/> methods can be
    /// used to check the type of error.
    /// </remarks>
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "By Design")]
    new ApiExceptionBase? Error { get; }

    /// <summary>Gets the HTTP response content headers as defined in RFC 2616.</summary>
    new HttpContentHeaders? ContentHeaders { get; }

    /// <summary>Gets a value indicating whether the request was successful.</summary>
    [MemberNotNullWhen(true, nameof(ContentHeaders))]
    [MemberNotNullWhen(false, nameof(Error))]
    new bool IsSuccessStatusCode { get; }

    /// <summary>
    /// Gets a value indicating whether the request was successful and there wasn't any other error (for example, during content deserialization).
    /// </summary>
    [MemberNotNullWhen(true, nameof(Content))]
    [MemberNotNullWhen(true, nameof(ContentHeaders))]
    [MemberNotNullWhen(false, nameof(Error))]
    new bool IsSuccessful { get; }

    /// <summary>Gets the deserialized request content as <typeparamref name="T"/>.</summary>
    T? Content { get; }
}
