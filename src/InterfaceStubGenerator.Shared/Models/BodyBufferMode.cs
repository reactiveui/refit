// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Describes how a generated body parameter chooses request-content buffering.</summary>
internal enum BodyBufferMode
{
    /// <summary>The method has no request body.</summary>
    None,

    /// <summary>The generated method should use <c>RefitSettings.Buffered</c> at runtime.</summary>
    Settings,

    /// <summary>The body is explicitly buffered.</summary>
    Buffered,

    /// <summary>The body is explicitly streamed.</summary>
    Streaming
}
