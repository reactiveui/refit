// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Refit.GeneratedCode.TestModels.Scenarios
{
    /// <summary>Exercises generated multipart request construction under the repository analyzer configuration.</summary>
    public interface IGeneratedMultipartApi
    {
        /// <summary>Uploads a raw stream part.</summary>
        /// <param name="stream">The stream to upload.</param>
        /// <returns>The response payload.</returns>
        [Multipart]
        [Post("/upload")]
        public Task<string> UploadStreamAsync(Stream stream);

        /// <summary>Uploads a stream, byte array and string part together.</summary>
        /// <param name="stream">The stream part to upload.</param>
        /// <param name="bytes">The byte array to upload.</param>
        /// <param name="text">The aliased string to upload.</param>
        /// <returns>The response payload.</returns>
        [Multipart]
        [Post("/upload")]
        public Task<string> UploadMixedAsync(
            StreamPart stream,
            byte[] bytes,
            [AliasAs("note")] string text);

        /// <summary>Uploads a byte array part using a custom multipart boundary.</summary>
        /// <param name="bytes">The byte array to upload.</param>
        /// <returns>The response payload.</returns>
        [Multipart("----CustomBoundary")]
        [Post("/upload")]
        public Task<string> UploadCustomBoundaryAsync([AliasAs("blob")] ByteArrayPart bytes);

        /// <summary>Uploads a collection of files alongside a single file and formattable identifier.</summary>
        /// <param name="files">The files to upload as one part each.</param>
        /// <param name="extra">An additional file to upload.</param>
        /// <param name="id">A formattable identifier rendered through the form formatter.</param>
        /// <returns>The response payload.</returns>
        [Multipart]
        [Post("/upload")]
        public Task<string> UploadFilesAsync(
            IEnumerable<FileInfo> files,
            FileInfo extra,
            [AliasAs("id")] Guid id);

        /// <summary>Uploads raw HTTP content directly.</summary>
        /// <param name="content">The HTTP content to upload.</param>
        /// <returns>The response payload.</returns>
        [Multipart]
        [Post("/upload")]
        public Task<string> UploadContentAsync(HttpContent content);

        /// <summary>Uploads a stream part alongside a path, header and request property that must not become parts.</summary>
        /// <param name="folder">The path segment.</param>
        /// <param name="token">The authorization header value.</param>
        /// <param name="trace">The request property value.</param>
        /// <param name="stream">The stream part to upload.</param>
        /// <returns>The response payload.</returns>
        [Multipart]
        [Post("/upload/{folder}")]
        public Task<string> UploadWithMetadataAsync(
            string folder,
            [Header("X-Token")] string token,
            [Property("Trace")] string trace,
            [AliasAs("file")] StreamPart stream);
    }
}
