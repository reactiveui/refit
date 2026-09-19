// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Generated HTTP reads for the shared person model.</summary>
internal interface IPeopleApi
{
    /// <summary>Reads one person from the route selected by their identifier.</summary>
    /// <param name="id">The identifier substituted into the path.</param>
    /// <param name="cancellationToken">Cancels the HTTP request and body read.</param>
    /// <returns>The deserialized person body.</returns>
    [Get("/people/{id}")]
    Task<Person> GetPersonAsync(int id, CancellationToken cancellationToken);
}
