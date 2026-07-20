// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Convenience helpers for working with <see cref="IApiResponse"/> and <see cref="IApiResponse{T}"/>.</summary>
public static class ApiResponseExtensions
{
    /// <summary>Success-guard helpers on the non-generic <see cref="IApiResponse"/>.</summary>
    /// <param name="response">The response to guard.</param>
    extension(IApiResponse response)
    {
        /// <summary>
        /// Ensures the request reached the server with a success status code, throwing the captured error otherwise.
        /// This makes the guard available on the interface without needing the concrete response type.
        /// </summary>
        /// <returns>The same response when the status code indicates success.</returns>
        /// <exception cref="ArgumentNullException">The response is <see langword="null"/>.</exception>
        /// <exception cref="ApiException">Thrown when an unsuccessful response was received from the server.</exception>
        /// <exception cref="ApiRequestException">Thrown when the request failed before receiving a response from the server.</exception>
        public ValueTask<IApiResponse> EnsureSuccessStatusCodeAsync()
        {
            var checkedResponse = response ?? throw new ArgumentNullException(nameof(response));
            return checkedResponse.IsSuccessStatusCode
                ? new(checkedResponse)
                : new(Task.FromException<IApiResponse>(GetError(checkedResponse)));
        }

        /// <summary>
        /// Ensures the request was successful and free of any other error (for example, during content
        /// deserialization), throwing the captured error otherwise.
        /// </summary>
        /// <returns>The same response when the request was fully successful.</returns>
        /// <exception cref="ArgumentNullException">The response is <see langword="null"/>.</exception>
        /// <exception cref="ApiException">Thrown when an unsuccessful response was received from the server.</exception>
        /// <exception cref="ApiRequestException">Thrown when the request failed before receiving a response from the server.</exception>
        public ValueTask<IApiResponse> EnsureSuccessfulAsync()
        {
            var checkedResponse = response ?? throw new ArgumentNullException(nameof(response));
            return checkedResponse.IsSuccessful
                ? new(checkedResponse)
                : new(Task.FromException<IApiResponse>(GetError(checkedResponse)));
        }
    }

    /// <summary>Success-guard helpers on <see cref="IApiResponse{T}"/>.</summary>
    /// <typeparam name="T">The deserialized response content type.</typeparam>
    /// <param name="response">The response to guard.</param>
    extension<T>(IApiResponse<T> response)
    {
        /// <summary>
        /// Ensures the request reached the server with a success status code, throwing the captured error otherwise.
        /// This makes the guard available on the interface without needing the concrete <see cref="ApiResponse{T}"/> type.
        /// </summary>
        /// <returns>The same response when the status code indicates success.</returns>
        /// <exception cref="ArgumentNullException">The response is <see langword="null"/>.</exception>
        /// <exception cref="ApiException">Thrown when an unsuccessful response was received from the server.</exception>
        /// <exception cref="ApiRequestException">Thrown when the request failed before receiving a response from the server.</exception>
        public ValueTask<IApiResponse<T>> EnsureSuccessStatusCodeAsync()
        {
            var checkedResponse = response ?? throw new ArgumentNullException(nameof(response));
            return checkedResponse.IsSuccessStatusCode
                ? new(checkedResponse)
                : new(Task.FromException<IApiResponse<T>>(GetError(checkedResponse)));
        }

        /// <summary>
        /// Ensures the request was successful and free of any other error (for example, during content
        /// deserialization), throwing the captured error otherwise.
        /// </summary>
        /// <returns>The same response when the request was fully successful.</returns>
        /// <exception cref="ArgumentNullException">The response is <see langword="null"/>.</exception>
        /// <exception cref="ApiException">Thrown when an unsuccessful response was received from the server.</exception>
        /// <exception cref="ApiRequestException">Thrown when the request failed before receiving a response from the server.</exception>
        public ValueTask<IApiResponse<T>> EnsureSuccessfulAsync()
        {
            var checkedResponse = response ?? throw new ArgumentNullException(nameof(response));
            return checkedResponse.IsSuccessful
                ? new(checkedResponse)
                : new(Task.FromException<IApiResponse<T>>(GetError(checkedResponse)));
        }
    }

    /// <summary>Gets the captured error, or a fallback when an unsuccessful response did not record one.</summary>
    /// <param name="response">The unsuccessful response.</param>
    /// <returns>The exception to surface to the caller.</returns>
    internal static Exception GetError(IApiResponse response) =>
        (Exception?)response.Error
        ?? new InvalidOperationException("The response was unsuccessful but did not capture an error.");
}
