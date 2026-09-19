// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;

namespace Refit.Documentation;

/// <summary>Checks each protected exception constructor through private contextual subclasses.</summary>
internal static class ExceptionConstructors
{
    /// <summary>The custom response failure message.</summary>
    private const string ReplyFailure = "App reply failure.";

    /// <summary>Checks retained context, default messages and optional inner causes.</summary>
    /// <param name="settings">The generated-metadata settings retained by each error.</param>
    internal static void Run(RefitSettings settings)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "https://people.example/people");
        using HttpResponseMessage reply = new(HttpStatusCode.BadRequest) { ReasonPhrase = "Rejected" };
        InvalidOperationException cause = new("Transport failed.");

        ContextError fromCause = new(request, settings, cause);
        ContextError fromMessage = new("App failure.", request, settings);
        ContextError fromBoth = new("App failure.", request, settings, cause);
        ReplyError fromReply = new(request, reply, settings);
        ReplyError replyWithCause = new(request, reply, settings, cause);
        ReplyError customReply = new(ReplyFailure, request, reply, settings);
        ReplyError customWithCause = new(ReplyFailure, request, reply, settings, cause);
        Console.WriteLine(fromCause.Message); // Transport failed.
        Console.WriteLine(customWithCause.StatusCode); // BadRequest

        SampleCheck.Equal(cause.Message, fromCause.Message);
        SampleCheck.Equal(null, fromMessage.InnerException);
        SampleCheck.Equal(cause, fromBoth.InnerException);
        SampleCheck.Equal("Response status code does not indicate success: 400 (Rejected).", fromReply.Message);
        SampleCheck.Equal(cause, replyWithCause.InnerException);
        SampleCheck.Equal(ReplyFailure, customReply.Message);
        SampleCheck.Equal(cause, customWithCause.InnerException);
        SampleCheck.Equal(request, customWithCause.RequestMessage);
        SampleCheck.Equal(settings, customWithCause.RefitSettings);
    }

    /// <summary>Exposes the three protected base-constructor paths to this sample.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1032",
        Justification = "The immediate base requires real request, method and serializer context and has no standard constructors; this subclass demonstrates that protected contract.")]

    [System.Diagnostics.DebuggerDisplay("{Message,nq}")]
    private sealed class ContextError : ApiExceptionBase
    {
        /// <summary>Initializes a new instance of the <see cref="ContextError"/> class.</summary>
        /// <param name="request">The originating request.</param>
        /// <param name="settings">The client settings.</param>
        /// <param name="cause">The failure supplying the message.</param>
        public ContextError(HttpRequestMessage request, RefitSettings settings, Exception cause)
            : base(request, HttpMethod.Get, settings, cause)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ContextError"/> class.</summary>
        /// <param name="message">The app-defined message.</param>
        /// <param name="request">The originating request.</param>
        /// <param name="settings">The client settings.</param>
        public ContextError(string message, HttpRequestMessage request, RefitSettings settings)
            : base(message, request, HttpMethod.Get, settings)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ContextError"/> class.</summary>
        /// <param name="message">The app-defined message.</param>
        /// <param name="request">The originating request.</param>
        /// <param name="settings">The client settings.</param>
        /// <param name="cause">The retained inner cause.</param>
        public ContextError(string message, HttpRequestMessage request, RefitSettings settings, Exception cause)
            : base(message, request, HttpMethod.Get, settings, cause)
        {
        }
    }

    /// <summary>Exposes each protected response-constructor path using actual reply context.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1032",
        Justification = "The immediate base requires real request, reply and serializer context and has no standard constructors; this subclass demonstrates that protected contract.")]

    [System.Diagnostics.DebuggerDisplay("{StatusCode}")]
    private sealed class ReplyError : ApiException
    {
        /// <summary>Initializes a new instance of the <see cref="ReplyError"/> class.</summary>
        /// <param name="request">The originating request.</param>
        /// <param name="reply">The reply supplying status, reason and headers.</param>
        /// <param name="settings">The client settings.</param>
        public ReplyError(HttpRequestMessage request, HttpResponseMessage reply, RefitSettings settings)
            : base(request, HttpMethod.Get, null, reply.StatusCode, reply.ReasonPhrase, reply.Headers, settings)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ReplyError"/> class.</summary>
        /// <param name="request">The originating request.</param>
        /// <param name="reply">The reply supplying status, reason and headers.</param>
        /// <param name="settings">The client settings.</param>
        /// <param name="cause">The retained inner cause.</param>
        public ReplyError(HttpRequestMessage request, HttpResponseMessage reply, RefitSettings settings, Exception cause)
            : base(request, HttpMethod.Get, null, reply.StatusCode, reply.ReasonPhrase, reply.Headers, settings, cause)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ReplyError"/> class.</summary>
        /// <param name="message">The app-defined message.</param>
        /// <param name="request">The originating request.</param>
        /// <param name="reply">The reply supplying status, reason and headers.</param>
        /// <param name="settings">The client settings.</param>
        public ReplyError(string message, HttpRequestMessage request, HttpResponseMessage reply, RefitSettings settings)
            : base(message, request, HttpMethod.Get, null, reply.StatusCode, reply.ReasonPhrase, reply.Headers, settings)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ReplyError"/> class.</summary>
        /// <param name="message">The app-defined message.</param>
        /// <param name="request">The originating request.</param>
        /// <param name="reply">The reply supplying status, reason and headers.</param>
        /// <param name="settings">The client settings.</param>
        /// <param name="cause">The retained inner cause.</param>
        public ReplyError(string message, HttpRequestMessage request, HttpResponseMessage reply, RefitSettings settings, Exception cause)
            : base(message, request, HttpMethod.Get, null, reply.StatusCode, reply.ReasonPhrase, reply.Headers, settings, cause)
        {
        }
    }
}
