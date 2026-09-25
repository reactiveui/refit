// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Streaming calls exercised by the controllable test responses.</summary>
internal interface ITestingStreamingApi
{
    /// <summary>Reads people one at a time as the response body arrives.</summary>
    /// <param name="cancellationToken">The token that cancels the request and the body read.</param>
    /// <returns>The people parsed from the streamed body.</returns>
    [Get("/people/live")]
    IAsyncEnumerable<TestingPerson> WatchAsync(CancellationToken cancellationToken);

    /// <summary>Uploads people as JSON Lines, one serialized person per line.</summary>
    /// <param name="people">The people to upload; enumerated while the body is written.</param>
    /// <returns>A task that completes when the upload has been answered.</returns>
    [Post("/people/import")]
    Task ImportAsync([Body(BodySerializationMethod.JsonLines)] IEnumerable<TestingPerson> people);

    /// <summary>Uploads people as JSON Lines from an asynchronous producer, one serialized person per line as it arrives.</summary>
    /// <param name="people">The people to upload; enumerated once while the body is written.</param>
    /// <param name="cancellationToken">The token that flows to the producer and to every write.</param>
    /// <returns>A task that completes when the upload has been answered.</returns>
    [Post("/people/import-live")]
    Task ImportLiveAsync([Body(BodySerializationMethod.JsonLines)] IAsyncEnumerable<TestingPerson> people, CancellationToken cancellationToken);
}
