// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>The people API whose calls the telemetry example traces and measures.</summary>
internal interface ITelemetryPeopleApi
{
    /// <summary>Reads one person.</summary>
    /// <param name="id">The person's identifier.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The person.</returns>
    [Get("/people/{id}")]
    Task<Person> GetPersonAsync(int id, CancellationToken cancellationToken);
}
