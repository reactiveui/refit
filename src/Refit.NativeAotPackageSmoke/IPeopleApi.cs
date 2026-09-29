// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.NativeAotPackageSmoke;

/// <summary>The smoke API. Every method must generate: RefitRequireGeneratedRequests fails the build otherwise.</summary>
public interface IPeopleApi
{
    /// <summary>Reads one person.</summary>
    /// <param name="id">The person's identifier.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The person.</returns>
    [Get("/people/{id}")]
    Task<Person> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>Reads one person with the response status and headers.</summary>
    /// <param name="id">The person's identifier.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The response wrapper.</returns>
    [Get("/people/{id}")]
    Task<ApiResponse<Person>> GetResponseAsync(int id, CancellationToken cancellationToken);

    /// <summary>Reads one person when the returned call is invoked.</summary>
    /// <param name="id">The person's identifier.</param>
    /// <returns>The deferred call.</returns>
    [Get("/people/{id}")]
    DeferredCall<Person> GetLater(int id);

    /// <summary>Searches people with a custom query converter.</summary>
    /// <param name="search">The search, written by <see cref="PersonSearchConverter"/>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The matching people.</returns>
    [Get("/people")]
    Task<List<Person>> SearchAsync([QueryConverter(typeof(PersonSearchConverter))] PersonSearch search, CancellationToken cancellationToken);

    /// <summary>Uploads a photo with JSON metadata.</summary>
    /// <param name="id">The person's identifier.</param>
    /// <param name="photo">The photo bytes.</param>
    /// <param name="metadata">The metadata, serialized as a JSON part.</param>
    /// <returns>The server's receipt text.</returns>
    [Multipart]
    [Post("/people/{id}/photo")]
    Task<string> UploadPhotoAsync(int id, [AliasAs("photo")] StreamPart photo, PhotoMetadata metadata);

    /// <summary>Streams people as a JSON array, JSON Lines or server-sent events, chosen by the reply's content type.</summary>
    /// <param name="cancellationToken">Cancels the stream.</param>
    /// <returns>The people as they arrive.</returns>
    [Get("/people/live")]
    IAsyncEnumerable<Person> WatchAsync(CancellationToken cancellationToken);

    /// <summary>Uploads people as JSON Lines while they are produced.</summary>
    /// <param name="people">The people to upload.</param>
    /// <param name="cancellationToken">Cancels the upload.</param>
    /// <returns>A task that completes when the server replies.</returns>
    [Post("/people/import")]
    Task ImportAsync([Body(BodySerializationMethod.JsonLines)] IAsyncEnumerable<Person> people, CancellationToken cancellationToken);

    /// <summary>Lists every person, following the page cursor.</summary>
    /// <param name="cursor">The cursor for the page to read; the enumeration supplies it after the first page.</param>
    /// <returns>The people across every page.</returns>
    [Get("/people/pages")]
    [Paged(Next = nameof(PersonPage.Next))]
    PagedEnumerable<PersonPage, Person> ListAll([PageToken] string? cursor = null);

    /// <summary>Reads a reply the JSON context does not describe.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>Never returns: reading the reply fails.</returns>
    [Get("/undescribed")]
    Task<Undescribed> GetUndescribedAsync(CancellationToken cancellationToken);
}
