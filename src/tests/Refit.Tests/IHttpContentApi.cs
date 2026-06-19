// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface that posts and returns <see cref="HttpContent"/> bodies.</summary>
public interface IHttpContentApi
{
    /// <summary>Posts an HTTP content body and returns the response content.</summary>
    /// <param name="content">The content to upload.</param>
    /// <returns>The response content.</returns>
    [Post("/blah")]
    Task<HttpContent> PostFileUpload([Body] HttpContent content);

    /// <summary>Posts an HTTP content body and returns the response content with metadata.</summary>
    /// <param name="content">The content to upload.</param>
    /// <returns>The API response wrapping the content.</returns>
    [Post("/blah")]
    Task<ApiResponse<HttpContent>> PostFileUploadWithMetadata([Body] HttpContent content);
}
