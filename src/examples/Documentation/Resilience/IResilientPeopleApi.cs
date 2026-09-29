// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>The people API a resilient client calls.</summary>
internal interface IResilientPeopleApi
{
    /// <summary>Reads one person. A GET is safe to retry.</summary>
    /// <param name="id">The person's identifier.</param>
    /// <param name="cancellationToken">Cancels the call, including any retry that is waiting.</param>
    /// <returns>The person.</returns>
    [Get("/people/{id}")]
    Task<Person> GetPersonAsync(int id, CancellationToken cancellationToken);

    /// <summary>Creates a person. A POST is not retried, because the service could create the person twice.</summary>
    /// <param name="person">The person to create.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The created person.</returns>
    [Post("/people")]
    Task<Person> CreatePersonAsync([Body] Person person, CancellationToken cancellationToken);
}
