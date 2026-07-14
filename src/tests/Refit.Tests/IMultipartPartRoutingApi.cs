// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Refit API surface exercising multipart part versus query routing.</summary>
public interface IMultipartPartRoutingApi
{
    /// <summary>Uploads a plain string as a multipart part.</summary>
    /// <param name="file">The multipart part value.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/plain")]
    Task<HttpResponseMessage> UploadPlainPart([AliasAs("file")] string file);

    /// <summary>Uploads a multipart part alongside a query-attributed argument.</summary>
    /// <param name="tag">The query-attributed argument routed to the query string.</param>
    /// <param name="file">The multipart part value.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/query")]
    Task<HttpResponseMessage> UploadWithQueryParam([Query] string tag, [AliasAs("file")] string file);

    /// <summary>Uploads a multipart part alongside an object whose property binds to the path.</summary>
    /// <param name="request">The object whose <see cref="MultipartRoutingRequest.Id"/> binds to the path.</param>
    /// <param name="file">The multipart part value.</param>
    /// <returns>The HTTP response message.</returns>
    [Multipart]
    [Post("/object/{request.Id}")]
    Task<HttpResponseMessage> UploadWithPathBoundObject(MultipartRoutingRequest request, [AliasAs("file")] string file);
}
