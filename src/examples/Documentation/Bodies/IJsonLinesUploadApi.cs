// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Uploads bulk-import records and audit events as JSON Lines, one serialized element per line.</summary>
internal interface IJsonLinesUploadApi
{
    /// <summary>Uploads records as an asynchronous producer makes them available, writing each one as it arrives.</summary>
    /// <param name="records">The records to upload; enumerated once while the body is written.</param>
    /// <param name="cancellationToken">The token that flows to the producer and to every write.</param>
    /// <returns>A task that completes when the upload has been answered.</returns>
    [Post("/imports/records")]
    Task ImportRecordsAsync(
        [Body(BodySerializationMethod.JsonLines)] IAsyncEnumerable<ImportRecord> records,
        CancellationToken cancellationToken);

    /// <summary>Uploads a materialized batch of records as JSON Lines.</summary>
    /// <param name="records">The records to upload; re-enumerated if the request is sent again.</param>
    /// <returns>A task that completes when the upload has been answered.</returns>
    [Post("/imports/records")]
    Task ImportRecordBatchAsync([Body(BodySerializationMethod.JsonLines)] IEnumerable<ImportRecord> records);

    /// <summary>
    /// Uploads caller-built content without further serialization. This is the escape hatch for forcing a declared
    /// element type Refit would not otherwise infer, such as a polymorphic base type.
    /// </summary>
    /// <param name="content">The caller-built JSON Lines content.</param>
    /// <returns>A task that completes when the upload has been answered.</returns>
    [Post("/imports/events")]
    Task ImportRawAsync([Body] HttpContent content);
}
