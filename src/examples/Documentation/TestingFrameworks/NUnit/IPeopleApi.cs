// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.TestingFrameworks;

/// <summary>
/// The API these tests pretend to call. Refit turns this interface into real HTTP calls; in these tests,
/// a stub handler intercepts every call instead of sending it over the network.
/// </summary>
public interface IPeopleApi
{
    /// <summary>Gets one person by id.</summary>
    /// <param name="id">The person's id.</param>
    /// <returns>The person the server sent back.</returns>
    [Get("/people/{id}")]
    Task<Person> GetPersonAsync(int id);

    /// <summary>Creates a person.</summary>
    /// <param name="person">The person to create.</param>
    /// <returns>The person the server saved.</returns>
    [Post("/people")]
    Task<Person> CreatePersonAsync([Body] Person person);

    /// <summary>Watches a live feed of people, one at a time, as they arrive.</summary>
    /// <param name="cancellationToken">Stops watching when cancelled.</param>
    /// <returns>Each person, as soon as the server sends them.</returns>
    [Get("/people/live")]
    IAsyncEnumerable<Person> WatchPeopleAsync(CancellationToken cancellationToken);

    /// <summary>Uploads a list of people, one per line, as JSON Lines.</summary>
    /// <param name="people">The people to upload.</param>
    /// <returns>A task that completes once the server has answered.</returns>
    [Post("/people/import")]
    Task ImportPeopleAsync([Body(BodySerializationMethod.JsonLines)] IEnumerable<Person> people);
}
