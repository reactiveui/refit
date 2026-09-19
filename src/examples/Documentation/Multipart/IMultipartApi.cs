// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.IO;

namespace Refit.Documentation;

/// <summary>Declares multipart requests built entirely by generated code.</summary>
internal interface IMultipartApi
{
    /// <summary>Sends a named file, text field and separate query value.</summary>
    /// <param name="file">The caller-owned stream with file metadata.</param>
    /// <param name="title">The plain-text form field.</param>
    /// <param name="mode">The query value, excluded from the form body.</param>
    /// <returns>The reply, which the caller must dispose.</returns>
    [Multipart("sample-boundary")]
    [Post("/upload")]
    Task<HttpResponseMessage> UploadAsync([AliasAs("file")] StreamPart file, string title, [Query] string mode);

    /// <summary>Sends bytes, an owned file stream and repeated attachments.</summary>
    /// <param name="bytes">The bytes with explicit multipart metadata.</param>
    /// <param name="file">The file opened by Refit while constructing content.</param>
    /// <param name="attachments">The items emitted as repeated parts under the same name.</param>
    /// <returns>The reply, which the caller must dispose.</returns>
    [Multipart]
    [Post("/files")]
    Task<HttpResponseMessage> UploadFilesAsync(ByteArrayPart bytes, FileInfoPart file, IEnumerable<ByteArrayPart> attachments);

    /// <summary>Sends raw content and file-like values without part wrappers.</summary>
    /// <param name="content">Content whose existing disposition headers are preserved.</param>
    /// <param name="raw">The stream that remains owned by the caller.</param>
    /// <param name="bytes">The bytes using the parameter name as file name.</param>
    /// <param name="file">The file using its filesystem name as transmitted file name.</param>
    /// <returns>The reply, which the caller must dispose.</returns>
    [Multipart]
    [Post("/raw")]
    Task<HttpResponseMessage> UploadRawAsync(HttpContent content, Stream raw, byte[] bytes, FileInfo file);

    /// <summary>Sends one JSON model part alongside a plain-text Guid.</summary>
    /// <param name="metadata">The sealed model registered with the JSON context.</param>
    /// <param name="token">The Guid rendered through the form formatter without JSON quotes.</param>
    /// <returns>The reply, which the caller must dispose.</returns>
    [Multipart]
    [Post("/metadata")]
    Task<HttpResponseMessage> UploadMetadataAsync(UploadMetadata metadata, Guid token);

    /// <summary>Builds an unsent request from a custom multipart extension.</summary>
    /// <param name="item">The custom part whose explicit name overrides the alias.</param>
    /// <returns>The built request, which the caller must dispose.</returns>
    [Multipart]
    [Post("/custom")]
    Task<HttpRequestMessage> BuildAsync([AliasAs("aliased")] MultipartItem item);
}
