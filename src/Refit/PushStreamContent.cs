// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

// NOTICE
// This has been converted from:
// https://github.com/ASP-NET-MVC/aspnetwebstack/blob/d5188c8a75b5b26b09ab89bedfd7ee635ae2ff17/src/System.Net.Http.Formatting/PushStreamContent.cs
// to work on NET Standard 1.4
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace System.Net.Http
{
    /// <summary>
    /// Provides an <see cref="HttpContent"/> implementation that exposes an output <see cref="Stream"/>
    /// which can be written to directly. The ability to push data to the output stream differs from the
    /// <see cref="StreamContent"/> where data is pulled and not pushed.
    /// </summary>
    /// <remarks>
    /// https://github.com/ASP-NET-MVC/aspnetwebstack/blob/d5188c8a75b5b26b09ab89bedfd7ee635ae2ff17/src/System.Net.Http.Formatting/PushStreamContent.cs.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    internal class PushStreamContent : HttpContent
    {
        /// <summary>The action invoked when the output stream becomes available.</summary>
        private readonly Func<Stream, HttpContent, TransportContext?, Task> _onStreamAvailable;

        /// <summary>
        /// Initializes a new instance of the <see cref="PushStreamContent"/> class. The
        /// <paramref name="onStreamAvailable"/> action is called when an output stream
        /// has become available allowing the action to write to it directly. When the
        /// stream is closed, it will signal to the content that is has completed and the
        /// HTTP request or response will be completed.
        /// </summary>
        /// <param name="onStreamAvailable">The action to call when an output stream is available.</param>
        public PushStreamContent(Action<Stream, HttpContent, TransportContext?> onStreamAvailable)
            : this(Taskify(onStreamAvailable), (MediaTypeHeaderValue?)null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="PushStreamContent"/> class.</summary>
        /// <param name="onStreamAvailable">The action to call when an output stream is available. The stream is automatically
        /// closed when the return task is completed.</param>
        public PushStreamContent(
            Func<Stream, HttpContent, TransportContext?, Task> onStreamAvailable)
            : this(onStreamAvailable, (MediaTypeHeaderValue?)null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="PushStreamContent"/> class with the given media type.</summary>
        /// <param name="onStreamAvailable">The action to call when an output stream is available.</param>
        /// <param name="mediaType">The media type to use for the content.</param>
        public PushStreamContent(
            Action<Stream, HttpContent, TransportContext?> onStreamAvailable,
            string mediaType)
            : this(Taskify(onStreamAvailable), new MediaTypeHeaderValue(mediaType))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="PushStreamContent"/> class with the given media type.</summary>
        /// <param name="onStreamAvailable">The action to call when an output stream is available.</param>
        /// <param name="mediaType">The media type to use for the content.</param>
        public PushStreamContent(
            Func<Stream, HttpContent, TransportContext?, Task> onStreamAvailable,
            string mediaType)
            : this(onStreamAvailable, new MediaTypeHeaderValue(mediaType))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="PushStreamContent"/> class with the given <see cref="MediaTypeHeaderValue"/>.</summary>
        /// <param name="onStreamAvailable">The action to call when an output stream is available.</param>
        /// <param name="mediaType">The media type to use for the content.</param>
        public PushStreamContent(
            Action<Stream, HttpContent, TransportContext?> onStreamAvailable,
            MediaTypeHeaderValue? mediaType)
            : this(Taskify(onStreamAvailable), mediaType)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="PushStreamContent"/> class with the given <see cref="MediaTypeHeaderValue"/>.</summary>
        /// <param name="onStreamAvailable">The action to call when an output stream is available.</param>
        /// <param name="mediaType">The media type to use for the content.</param>
        public PushStreamContent(
            Func<Stream, HttpContent, TransportContext?, Task> onStreamAvailable,
            MediaTypeHeaderValue? mediaType)
        {
            _onStreamAvailable =
                onStreamAvailable ?? throw new ArgumentNullException(nameof(onStreamAvailable));
            Headers.ContentType = mediaType ?? new MediaTypeHeaderValue("application/octet-stream");
        }

        /// <summary>
        /// When this method is called, it calls the action provided in the constructor with the output
        /// stream to write to. Once the action has completed its work it closes the stream which will
        /// close this content instance and complete the HTTP request or response.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to which to write.</param>
        /// <param name="context">The associated <see cref="TransportContext"/>.</param>
        /// <returns>A <see cref="Task"/> instance that is asynchronously serializing the object's content.</returns>
        protected override async Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context)
        {
            var serializeToStreamTask = new TaskCompletionSource<bool>();

            var wrappedStream = new CompleteTaskOnCloseStream(stream, serializeToStreamTask);
            using (wrappedStream)
            {
                await _onStreamAvailable(wrappedStream, this, context).ConfigureAwait(false);

                // wait for wrappedStream.Close/Dispose to get called.
                await serializeToStreamTask.Task.ConfigureAwait(false);
            }
        }

        /// <summary>Computes the length of the stream if possible.</summary>
        /// <param name="length">The computed length of the stream.</param>
        /// <returns><c>true</c> if the length has been computed; otherwise <c>false</c>.</returns>
        protected override bool TryComputeLength(out long length)
        {
            // We can't know the length of the content being pushed to the output stream.
            length = -1;
            return false;
        }

        /// <summary>Wraps a synchronous stream callback in a task-returning callback.</summary>
        /// <param name="onStreamAvailable">The synchronous action to wrap.</param>
        /// <returns>A task-returning callback that invokes the action.</returns>
        private static Func<Stream, HttpContent, TransportContext?, Task> Taskify(
            Action<Stream, HttpContent, TransportContext?> onStreamAvailable)
        {
            if (onStreamAvailable is null)
            {
                throw new ArgumentNullException(nameof(onStreamAvailable));
            }

            return (stream, content, transportContext) =>
            {
                onStreamAvailable(stream, content, transportContext);

                // https://github.com/ASP-NET-MVC/aspnetwebstack/blob/5118a14040b13f95bf778d1fc4522eb4ea2eef18/src/Common/TaskHelpers.cs#L10
                return Task.FromResult<AsyncVoid>(default);
            };
        }

        /// <summary>Used as the T in a "conversion" of a Task into a Task{T}.</summary>
        /// <remarks>
        /// https://github.com/ASP-NET-MVC/aspnetwebstack/blob/5118a14040b13f95bf778d1fc4522eb4ea2eef18/src/Common/TaskHelpers.cs#L65.
        /// </remarks>
        private readonly struct AsyncVoid : IEquatable<AsyncVoid>
        {
            /// <inheritdoc/>
            public bool Equals(AsyncVoid other) => true;

            /// <inheritdoc/>
            public override bool Equals(object? obj) => obj is AsyncVoid;

            /// <inheritdoc/>
            public override int GetHashCode() => 0;
        }

        /// <summary>A delegating stream that signals completion when it is disposed.</summary>
        internal sealed class CompleteTaskOnCloseStream : Refit.DelegatingStream
        {
            /// <summary>The task signalled when the stream is closed.</summary>
            private readonly TaskCompletionSource<bool> _serializeToStreamTask;

            /// <summary>Initializes a new instance of the <see cref="CompleteTaskOnCloseStream"/> class.</summary>
            /// <param name="innerStream">The stream to delegate to.</param>
            /// <param name="serializeToStreamTask">The task to signal when the stream closes.</param>
            public CompleteTaskOnCloseStream(
                Stream innerStream,
                TaskCompletionSource<bool> serializeToStreamTask)
                : base(innerStream, ownsInnerStream: false) =>
                _serializeToStreamTask =
                    serializeToStreamTask
                    ?? throw new ArgumentNullException(nameof(serializeToStreamTask));

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                // We don't dispose the underlying stream because we don't own it. Dispose in this case just signifies
                // that the user's action is finished. The base class honours ownsInnerStream: false and therefore
                // does not dispose the inner stream.
                _serializeToStreamTask.TrySetResult(true);
                base.Dispose(disposing);
            }
        }
    }
}
