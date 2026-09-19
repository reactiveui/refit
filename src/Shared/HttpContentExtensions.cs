// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if !NET9_0_OR_GREATER
namespace Refit;

/// <summary>Polyfill extensions for HttpContent on older target frameworks.</summary>
internal static class HttpContentExtensions
{
    /// <summary>Polyfill members for <see cref="HttpContent"/> that check cancellation before starting an operation.</summary>
    /// <param name="httpContent">The content the polyfill members operate on.</param>
    /// <remarks>Older framework APIs cannot cancel these operations after they have started.</remarks>
    extension(HttpContent httpContent)
    {
        /// <summary>Loads the content into a buffer unless cancellation has already been requested.</summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the buffering operation.</returns>
        internal Task LoadIntoBufferAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : httpContent.LoadIntoBufferAsync();

#if !NET6_0_OR_GREATER
        /// <summary>Reads the content as a stream unless cancellation has already been requested.</summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task producing the content stream.</returns>
        internal Task<Stream> ReadAsStreamAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested
                ? Task.FromCanceled<Stream>(cancellationToken)
                : httpContent.ReadAsStreamAsync();

        /// <summary>Reads the content as a string unless cancellation has already been requested.</summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task producing the content string.</returns>
        internal Task<string> ReadAsStringAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested
                ? Task.FromCanceled<string>(cancellationToken)
                : httpContent.ReadAsStringAsync();
#endif
    }
}
#endif
