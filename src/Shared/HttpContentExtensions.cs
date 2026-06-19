// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if !NET9_0_OR_GREATER
namespace Refit;

/// <summary>Polyfill extensions for HttpContent on older target frameworks.</summary>
internal static class HttpContentExtensions
{
    /// <summary>Polyfill members for <see cref="HttpContent"/> that accept and ignore a cancellation token.</summary>
    /// <param name="httpContent">The content the polyfill members operate on.</param>
    extension(HttpContent httpContent)
    {
        /// <summary>Loads the content into a buffer, ignoring the cancellation token.</summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the buffering operation.</returns>
        public Task LoadIntoBufferAsync(CancellationToken cancellationToken) =>
            httpContent.LoadIntoBufferAsync();

#if !NET6_0_OR_GREATER
        /// <summary>Reads the content as a stream, ignoring the cancellation token.</summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task producing the content stream.</returns>
        public Task<Stream> ReadAsStreamAsync(CancellationToken cancellationToken) =>
            httpContent.ReadAsStreamAsync();

        /// <summary>Reads the content as a string, ignoring the cancellation token.</summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task producing the content string.</returns>
        public Task<string> ReadAsStringAsync(CancellationToken cancellationToken) =>
            httpContent.ReadAsStringAsync();
#endif
    }
}
#endif
