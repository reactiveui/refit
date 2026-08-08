// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Default Api exception factory.</summary>
/// <param name="refitSettings">The Refit settings used when building exceptions.</param>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public class DefaultApiExceptionFactory(RefitSettings refitSettings)
{
    /// <summary>Creates the asynchronous.</summary>
    /// <param name="responseMessage">The response message.</param>
    /// <returns>A task that yields the created exception, or null when the response was successful.</returns>
    public ValueTask<Exception?> CreateAsync(HttpResponseMessage responseMessage) =>
        responseMessage?.IsSuccessStatusCode == false
            ? CreateExceptionAsync(responseMessage, refitSettings)
            : default;

    /// <summary>Builds an <see cref="ApiException"/> for the given unsuccessful response.</summary>
    /// <param name="responseMessage">The response message.</param>
    /// <param name="refitSettings">The Refit settings.</param>
    /// <returns>The created exception.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="responseMessage"/> has no associated request message, which a custom <see cref="HttpMessageHandler"/> must set itself.</exception>
    internal static async ValueTask<Exception?> CreateExceptionAsync(
        HttpResponseMessage responseMessage,
        RefitSettings refitSettings)
    {
        var requestMessage =
            responseMessage.RequestMessage
            ?? throw new InvalidOperationException(
                "The HttpResponseMessage has no associated RequestMessage. When supplying a "
                + "custom HttpMessageHandler (for example in a test), ensure it sets "
                + "HttpResponseMessage.RequestMessage.");

        return await ApiException
            .Create(requestMessage, requestMessage.Method, responseMessage, refitSettings)
            .ConfigureAwait(false);
    }
}
