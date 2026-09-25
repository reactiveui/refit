// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Refit.GeneratedCode.TestModels.Scenarios
{
    /// <summary>Exercises generated JSON Lines request bodies, both typed-synchronous and asynchronous, under the repository analyzer configuration.</summary>
    public interface IGeneratedJsonLinesApi
    {
        /// <summary>Uploads a synchronous sequence of sealed log entries as JSON Lines, keeping the declared element type.</summary>
        /// <param name="entries">The entries to upload, one JSON document per line.</param>
        /// <returns>A task that completes when the upload finishes.</returns>
        [Post("/logs")]
        Task UploadEntriesAsync([Body(BodySerializationMethod.JsonLines)] IEnumerable<GeneratedLogEntry> entries);

        /// <summary>Uploads an asynchronous sequence of sealed log entries as JSON Lines, keeping the declared element type.</summary>
        /// <param name="entries">The entries to upload, one JSON document per line.</param>
        /// <param name="cancellationToken">A token that cancels the upload.</param>
        /// <returns>A task that completes when the upload finishes.</returns>
        [Post("/logs")]
        Task StreamEntriesAsync([Body(BodySerializationMethod.JsonLines)] IAsyncEnumerable<GeneratedLogEntry> entries, CancellationToken cancellationToken);
    }
}
