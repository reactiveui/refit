// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>Refit API surface exercised by the multipart upload tests.</summary>
public interface IRunscopeApi
{
    /// <summary>Uploads a raw <see cref="Stream"/> as multipart content.</summary>
    /// <param name="stream">The stream to upload.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadStream(Stream stream);

    /// <summary>Uploads a raw <see cref="Stream"/> using a custom multipart boundary.</summary>
    /// <param name="stream">The stream to upload.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart("-----SomeCustomBoundary")]
    [Post("/")]
    Task<HttpResponseMessage> UploadStreamWithCustomBoundary(Stream stream);

    /// <summary>Uploads a <see cref="StreamPart"/> as multipart content.</summary>
    /// <param name="stream">The stream part to upload.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadStreamPart(StreamPart stream);

    /// <summary>Uploads a <see cref="StreamPart"/> alongside query parameters.</summary>
    /// <param name="someQueryParams">The query parameters to append to the request URI.</param>
    /// <param name="stream">The stream part to upload.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadStreamPart(
        [Query] ModelObject someQueryParams,
        StreamPart stream);

    /// <summary>Uploads a byte array as multipart content.</summary>
    /// <param name="bytes">The bytes to upload.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadBytes(byte[] bytes);

    /// <summary>Uploads a <see cref="ByteArrayPart"/> as multipart content.</summary>
    /// <param name="bytes">The byte array part to upload.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadBytesPart(
        [AliasAs("ByteArrayPartParamAlias")] ByteArrayPart bytes);

    /// <summary>Uploads a string value as multipart content.</summary>
    /// <param name="someString">The string to upload.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadString([AliasAs("SomeStringAlias")] string someString);

    /// <summary>Uploads a <see cref="Guid"/> and a <see cref="DateTimeOffset"/> as multipart values.</summary>
    /// <param name="id">The identifier to upload.</param>
    /// <param name="timestamp">The timestamp to upload.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadFormattableValues(
        [AliasAs("id")] Guid id,
        [AliasAs("timestamp")] DateTimeOffset timestamp);

    /// <summary>Uploads a string value alongside a header and a request property.</summary>
    /// <param name="authorization">The authorization header value.</param>
    /// <param name="someProperty">The request property value.</param>
    /// <param name="someString">The string to upload.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadStringWithHeaderAndRequestProperty(
        [Header("Authorization")] string authorization,
        [Property("SomeProperty")] string someProperty,
        [AliasAs("SomeStringAlias")] string someString);

    /// <summary>Uploads a collection of <see cref="FileInfo"/> values and an additional file.</summary>
    /// <param name="fileInfos">The files to upload.</param>
    /// <param name="anotherFile">An additional file to upload.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadFileInfo(IEnumerable<FileInfo> fileInfos, FileInfo anotherFile);

    /// <summary>Uploads a collection of <see cref="FileInfoPart"/> values and an additional file part.</summary>
    /// <param name="fileInfos">The file parts to upload.</param>
    /// <param name="anotherFile">An additional file part to upload.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadFileInfoPart(
        IEnumerable<FileInfoPart> fileInfos,
        FileInfoPart anotherFile);

    /// <summary>Uploads a single object serialized as multipart content.</summary>
    /// <param name="theObject">The object to upload.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadJsonObject(ModelObject theObject);

    /// <summary>Uploads a collection of objects serialized as multipart content.</summary>
    /// <param name="theObjects">The objects to upload.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadJsonObjects(IEnumerable<ModelObject> theObjects);

    /// <summary>Uploads a mixture of object, file, enum, string and integer parts.</summary>
    /// <param name="theObjects">The objects to upload.</param>
    /// <param name="anotherModel">An additional model to upload.</param>
    /// <param name="file">A file to upload.</param>
    /// <param name="enumValue">An enum value to upload.</param>
    /// <param name="text">A string value to upload.</param>
    /// <param name="number">An integer value to upload.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadMixedObjects(
        IEnumerable<ModelObject> theObjects,
        AnotherModel anotherModel,
        [AliasAs("aFile")] FileInfo file,
        [AliasAs("anEnum")] AnEnum enumValue,
        [AliasAs("aString")] string text,
        [AliasAs("anInt")] int number);

    /// <summary>Uploads arbitrary <see cref="HttpContent"/> as multipart content.</summary>
    /// <param name="content">The HTTP content to upload.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/")]
    Task<HttpResponseMessage> UploadHttpContent(HttpContent content);
}
