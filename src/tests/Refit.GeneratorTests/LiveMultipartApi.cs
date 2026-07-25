// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.LiveMultipart;

/// <summary>Contains multipart models and the API generated into the test assembly.</summary>
public static partial class LiveMultipartApi
{
    /// <summary>Exercises generated multipart request construction.</summary>
    public interface ILiveMultipartApi
    {
        /// <summary>Uploads a flattened profile.</summary>
        /// <param name="profile">The profile.</param>
        /// <returns>The response body.</returns>
        [Multipart]
        [Post("/upload")]
        Task<string> UploadProfile([FormObject] Profile profile);

        /// <summary>Uploads a Boolean value.</summary>
        /// <param name="flag">The flag.</param>
        /// <returns>The response body.</returns>
        [Multipart]
        [Post("/upload")]
        Task<string> UploadFlag([AliasAs("flag")] bool flag);

        /// <summary>Uploads a serialized report.</summary>
        /// <param name="report">The report.</param>
        /// <returns>The response body.</returns>
        [Multipart]
        [Post("/upload")]
        Task<string> UploadReport([AliasAs("report")] Report report);

        /// <summary>Uploads a stream.</summary>
        /// <param name="stream">The stream.</param>
        /// <returns>The response body.</returns>
        [Multipart]
        [Post("/upload")]
        Task<string> UploadStream(Stream stream);

        /// <summary>Uploads a stream part.</summary>
        /// <param name="part">The stream part.</param>
        /// <returns>The response body.</returns>
        [Multipart]
        [Post("/upload")]
        Task<string> UploadStreamPart(StreamPart part);

        /// <summary>Uploads raw bytes.</summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns>The response body.</returns>
        [Multipart]
        [Post("/upload")]
        Task<string> UploadBytes(byte[] bytes);

        /// <summary>Uploads a byte-array part.</summary>
        /// <param name="part">The byte-array part.</param>
        /// <returns>The response body.</returns>
        [Multipart]
        [Post("/upload")]
        Task<string> UploadBytesPart([AliasAs("blob")] ByteArrayPart part);

        /// <summary>Uploads a string value.</summary>
        /// <param name="value">The string value.</param>
        /// <returns>The response body.</returns>
        [Multipart]
        [Post("/upload")]
        Task<string> UploadString([AliasAs("alias")] string value);

        /// <summary>Uploads a file-info part.</summary>
        /// <param name="part">The file-info part.</param>
        /// <returns>The response body.</returns>
        [Multipart]
        [Post("/upload")]
        Task<string> UploadFileInfoPart(FileInfoPart part);

        /// <summary>Uploads a file.</summary>
        /// <param name="file">The file.</param>
        /// <returns>The response body.</returns>
        [Multipart]
        [Post("/upload")]
        Task<string> UploadFile(FileInfo file);

        /// <summary>Uploads a file collection and one extra file.</summary>
        /// <param name="files">The files.</param>
        /// <param name="extra">The extra file.</param>
        /// <returns>The response body.</returns>
        [Multipart]
        [Post("/upload")]
        Task<string> UploadFiles(IEnumerable<FileInfo> files, FileInfo extra);

        /// <summary>Uploads a stream-part collection.</summary>
        /// <param name="parts">The stream parts.</param>
        /// <returns>The response body.</returns>
        [Multipart]
        [Post("/upload")]
        Task<string> UploadStreamParts(IEnumerable<StreamPart> parts);

        /// <summary>Uploads formatted values.</summary>
        /// <param name="id">The identifier.</param>
        /// <param name="at">The date and time.</param>
        /// <returns>The response body.</returns>
        [Multipart]
        [Post("/upload")]
        Task<string> UploadFormattable([AliasAs("id")] Guid id, [AliasAs("at")] DateTimeOffset at);

        /// <summary>Uploads bytes with a custom boundary.</summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns>The response body.</returns>
        [Multipart("----CustomBoundary")]
        [Post("/upload")]
        Task<string> UploadCustomBoundary(byte[] bytes);

        /// <summary>Uploads a part while keeping header, property, and path values out of the multipart body.</summary>
        /// <param name="folder">The path folder.</param>
        /// <param name="token">The header token.</param>
        /// <param name="trace">The request property.</param>
        /// <param name="part">The file part.</param>
        /// <returns>The response body.</returns>
        [Multipart]
        [Post("/upload/{folder}")]
        Task<string> UploadWithHeaderPropertyPath(
            string folder,
            [Header("X-Token")] string token,
            [Property("Trace")] string trace,
            [AliasAs("file")] StreamPart part);
    }
}
