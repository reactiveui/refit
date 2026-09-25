// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A Refit interface uploading an asynchronous sequence as JSON Lines, used to exercise a generated client running on a source-generated JSON context.</summary>
internal interface IJsonLinesUploadApi
{
    /// <summary>Uploads a sequence of records as JSON Lines (newline-delimited JSON).</summary>
    /// <param name="records">The records to upload, one JSON document per line.</param>
    /// <param name="cancellationToken">A token that cancels the upload.</param>
    /// <returns>A task that completes when the upload finishes.</returns>
    [Post("/upload")]
    Task Upload([Body(BodySerializationMethod.JsonLines)] IAsyncEnumerable<UploadRecord> records, CancellationToken cancellationToken);
}
