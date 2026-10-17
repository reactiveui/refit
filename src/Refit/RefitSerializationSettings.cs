// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Configures serialization for a Refit client.</summary>
[System.Diagnostics.DebuggerDisplay("RefitSerializationSettings")]
public sealed class RefitSerializationSettings
{
    /// <summary>Gets or sets how requests' content should be serialized. (defaults to <see cref="SystemTextJsonContentSerializer"/>).</summary>
    public IHttpContentSerializer ContentSerializer { get; set; } = new SystemTextJsonContentSerializer();

    /// <summary>Gets or sets the <see cref="IFormUrlEncodedParameterFormatter"/> instance to use (defaults to <see cref="DefaultFormUrlEncodedParameterFormatter"/>).</summary>
    public IFormUrlEncodedParameterFormatter FormUrlEncodedParameterFormatter { get; set; } = new DefaultFormUrlEncodedParameterFormatter();

    /// <summary>
    /// Gets or sets a value indicating whether the request's body content is buffered before sending.
    /// Defaults to false. When true, Refit loads the content into a buffer before sending it.
    /// </summary>
    public bool Buffered { get; set; }

    /// <summary>
    /// Gets or sets how JSON request bodies are serialized (defaults to <see cref="RequestBodySerializationMode.Default"/>).
    /// <see cref="RequestBodySerializationMode.Buffered"/> and <see cref="RequestBodySerializationMode.Streamed"/> use
    /// the synchronous serialization path so the System.Text.Json source-generated fast-path can engage; both require
    /// the configured <see cref="ContentSerializer"/> to implement <see cref="ISynchronousContentSerializer"/>.
    /// </summary>
    public RequestBodySerializationMode RequestBodySerialization { get; set; } = RequestBodySerializationMode.Default;

    /// <summary>
    /// Gets or sets the content coding applied to every request body (defaults to <see cref="RequestCompression.None"/>).
    /// A <c>[Body]</c> parameter naming its own coding overrides this, and <see cref="RequestCompression.None"/> on the
    /// parameter opts that one method out.
    /// </summary>
    /// <remarks>Only turn this on against a server that accepts a compressed request body; there is no negotiation for it.</remarks>
    public RequestCompression RequestCompression { get; set; } = RequestCompression.None;

    /// <summary>Gets or sets how hard <see cref="RequestCompression"/> compresses (defaults to <see cref="System.IO.Compression.CompressionLevel.Optimal"/>).</summary>
    public System.IO.Compression.CompressionLevel RequestCompressionLevel { get; set; } = System.IO.Compression.CompressionLevel.Optimal;

#if NET9_0_OR_GREATER
    /// <summary>Gets or sets the per-coding compressor settings, or null to compress by level alone.</summary>
    /// <remarks>Options set for a coding override the level for that coding, including where a <c>[Body]</c> parameter
    /// named the coding and its own level.</remarks>
    public RequestCompressionOptions? RequestCompressionOptions { get; set; }

#endif
}
