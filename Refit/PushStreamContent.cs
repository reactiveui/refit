// Copyright(c) Microsoft Open Technologies, Inc.All rights reserved.
// Microsoft Open Technologies would like to thank its contributors, a list
// of whom are at http://aspnetwebstack.codeplex.com/wikipage?title=Contributors.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License. You may
// obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied. See the License for the specific language governing permissions
// and limitations under the License.

// NOTICE
// This has been converted from:
// https://github.com/ASP-NET-MVC/aspnetwebstack/blob/d5188c8a75b5b26b09ab89bedfd7ee635ae2ff17/src/System.Net.Http.Formatting/PushStreamContent.cs
// to work on NET Standard 1.4

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>
    /// Provides an <see cref="HttpContent"/> implementation that exposes an output <see cref="Stream"/>
    /// which can be written to directly. The ability to push data to the output stream differs from the 
    /// <see cref="StreamContent"/> where data is pulled and not pushed.
    /// </summary>
    // https://github.com/ASP-NET-MVC/aspnetwebstack/blob/d5188c8a75b5b26b09ab89bedfd7ee635ae2ff17/src/System.Net.Http.Formatting/PushStreamContent.cs
    [ExcludeFromCodeCoverage]
    class PushStreamContent : HttpContent
    {
        readonly Func<Stream, HttpContent, TransportContext, Task> _onStreamAvailable;

        /// <summary>
        /// Initializes a new instance of the <see cref="PushStreamContent"/> class. The
        /// <paramref name="onStreamAvailable"/> action is called when an output stream
        /// has become available allowing the action to write to it directly. When the 
        /// stream is closed, it will signal to the content that is has completed and the 
        /// HTTP request or response will be completed.
        /// </summary>
        /// <param name="onStreamAvailable">The action to call when an output stream is available.</param>
        public PushStreamContent(Action<Stream, HttpContent, TransportContext> onStreamAvailable)
            : this(Taskify(onStreamAvailable), (MediaTypeHeaderValue)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PushStreamContent"/> class. 
        /// </summary>
        /// <param name="onStreamAvailable">The action to call when an output stream is available. The stream is automatically
        /// closed when the return task is completed.</param>
        public PushStreamContent(Func<Stream, HttpContent, TransportContext, Task> onStreamAvailable)
            : this(onStreamAvailable, (MediaTypeHeaderValue)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PushStreamContent"/> class with the given media type.
        /// </summary>
        public PushStreamContent(Action<Stream, HttpContent, TransportContext> onStreamAvailable, string mediaType)
            : this(Taskify(onStreamAvailable), new MediaTypeHeaderValue(mediaType))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PushStreamContent"/> class with the given media type.
        /// </summary>
        public PushStreamContent(Func<Stream, HttpContent, TransportContext, Task> onStreamAvailable, string mediaType)
            : this(onStreamAvailable, new MediaTypeHeaderValue(mediaType))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PushStreamContent"/> class with the given <see cref="MediaTypeHeaderValue"/>.
        /// </summary>
        public PushStreamContent(Action<Stream, HttpContent, TransportContext> onStreamAvailable, MediaTypeHeaderValue mediaType)
            : this(Taskify(onStreamAvailable), mediaType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PushStreamContent"/> class with the given <see cref="MediaTypeHeaderValue"/>.
        /// </summary>
        public PushStreamContent(Func<Stream, HttpContent, TransportContext, Task> onStreamAvailable, MediaTypeHeaderValue mediaType)
        {
            _onStreamAvailable = onStreamAvailable ?? throw new ArgumentNullException(nameof(onStreamAvailable));
            Headers.ContentType = mediaType ?? new MediaTypeHeaderValue("application/octet-stream");
        }

        static Func<Stream, HttpContent, TransportContext, Task> Taskify(
            Action<Stream, HttpContent, TransportContext> onStreamAvailable)
        {
            if (onStreamAvailable == null)
            {
                throw new ArgumentNullException(nameof(onStreamAvailable));
            }

            return (Stream stream, HttpContent content, TransportContext transportContext) =>
            {
                onStreamAvailable(stream, content, transportContext);
                // https://github.com/ASP-NET-MVC/aspnetwebstack/blob/5118a14040b13f95bf778d1fc4522eb4ea2eef18/src/Common/TaskHelpers.cs#L10
                return Task.FromResult<AsyncVoid>(default);
            };
        }

        /// <summary>
        /// Used as the T in a "conversion" of a Task into a Task{T}
        /// </summary>
        // https://github.com/ASP-NET-MVC/aspnetwebstack/blob/5118a14040b13f95bf778d1fc4522eb4ea2eef18/src/Common/TaskHelpers.cs#L65
        struct AsyncVoid
        {
        }

        /// <summary>
        /// When this method is called, it calls the action provided in the constructor with the output 
        /// stream to write to. Once the action has completed its work it closes the stream which will 
        /// close this content instance and complete the HTTP request or response.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to which to write.</param>
        /// <param name="context">The associated <see cref="TransportContext"/>.</param>
        /// <returns>A <see cref="Task"/> instance that is asynchronously serializing the object's content.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception is passed as task result.")]
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var serializeToStreamTask = new TaskCompletionSource<bool>();

            Stream wrappedStream = new CompleteTaskOnCloseStream(stream, serializeToStreamTask);
            await _onStreamAvailable(wrappedStream, this, context);

            // wait for wrappedStream.Close/Dispose to get called.
            await serializeToStreamTask.Task;
        }

        /// <summary>
        /// Computes the length of the stream if possible.
        /// </summary>
        /// <param name="length">The computed length of the stream.</param>
        /// <returns><c>true</c> if the length has been computed; otherwise <c>false</c>.</returns>
        protected override bool TryComputeLength(out long length)
        {
            // We can't know the length of the content being pushed to the output stream.
            length = -1;
            return false;
        }

        internal class CompleteTaskOnCloseStream : DelegatingStream
        {
            readonly TaskCompletionSource<bool> _serializeToStreamTask;

            public CompleteTaskOnCloseStream(Stream innerStream, TaskCompletionSource<bool> serializeToStreamTask)
                : base(innerStream)
            {
                Contract.Assert(serializeToStreamTask != null);
                _serializeToStreamTask = serializeToStreamTask;
            }

            [SuppressMessage(
                "Microsoft.Usage",
                "CA2215:Dispose methods should call base class dispose",
                Justification = "See comments, this is intentional.")]
            protected override void Dispose(bool disposing)
            {
                // We don't dispose the underlying stream because we don't own it. Dispose in this case just signifies
                // that the user's action is finished.
                _serializeToStreamTask.TrySetResult(true);
            }
        }
    }

    /// <summary>
    /// Stream that delegates to inner stream. 
    /// This is taken from System.Net.Http
    /// </summary>
    // https://github.com/ASP-NET-MVC/aspnetwebstack/blob/d5188c8a75b5b26b09ab89bedfd7ee635ae2ff17/src/System.Net.Http.Formatting/Internal/DelegatingStream.cs
    [ExcludeFromCodeCoverage]
    abstract class DelegatingStream : Stream
    {
        protected DelegatingStream(Stream innerStream)
        {
            InnerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        }

        protected Stream InnerStream { get; private set; }

        public override bool CanRead
        {
            get { return InnerStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return InnerStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return InnerStream.CanWrite; }
        }

        public override long Length
        {
            get { return InnerStream.Length; }
        }

        public override long Position
        {
            get { return InnerStream.Position; }
            set { InnerStream.Position = value; }
        }

        public override int ReadTimeout
        {
            get { return InnerStream.ReadTimeout; }
            set { InnerStream.ReadTimeout = value; }
        }

        public override bool CanTimeout
        {
            get { return InnerStream.CanTimeout; }
        }

        public override int WriteTimeout
        {
            get { return InnerStream.WriteTimeout; }
            set { InnerStream.WriteTimeout = value; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                InnerStream.Dispose();
            }
            base.Dispose(disposing);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return InnerStream.Seek(offset, origin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return InnerStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return InnerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int ReadByte()
        {
            return InnerStream.ReadByte();
        }

        public override void Flush()
        {
            InnerStream.Flush();
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return InnerStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return InnerStream.FlushAsync(cancellationToken);
        }

        public override void SetLength(long value)
        {
            InnerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            InnerStream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return InnerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            InnerStream.WriteByte(value);
        }
    }
}
