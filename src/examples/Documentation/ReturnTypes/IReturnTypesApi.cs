// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Declares task, value-task, observable and response-wrapper result forms.</summary>
internal interface IReturnTypesApi
{
    /// <summary>Sends a request and reads a person through a Task result.</summary>
    /// <param name="cancellationToken">The token that cancels request execution and response reading.</param>
    /// <returns>The person deserialized from the successful response.</returns>
    [Get("/person")]
    Task<Person> GetTaskAsync(CancellationToken cancellationToken);

    /// <summary>Sends a request and reads a person through a ValueTask result.</summary>
    /// <param name="cancellationToken">The token that cancels request execution and response reading.</param>
    /// <returns>The person deserialized from the successful response.</returns>
    [Get("/person")]
    ValueTask<Person> GetValueTaskAsync(CancellationToken cancellationToken);

    /// <summary>Returns a cold sequence that sends one request for each subscription.</summary>
    /// <param name="cancellationToken">The token that cancels request execution and response reading.</param>
    /// <returns>A cold sequence that sends a separate request for each subscription.</returns>
    [Get("/person")]
    IObservable<Person> GetPerson(CancellationToken cancellationToken);

    /// <summary>Returns a caller-owned response wrapper with both metadata and person content.</summary>
    /// <param name="cancellationToken">The token that cancels request execution and response reading.</param>
    /// <returns>The response wrapper, which the caller must dispose after inspection.</returns>
    [Get("/person")]
    Task<ApiResponse<Person>> GetResponseAsync(CancellationToken cancellationToken);

    /// <summary>Sends a request whose successful response body is ignored.</summary>
    /// <param name="cancellationToken">The token that cancels request execution and response reading.</param>
    /// <returns>A task that completes after the sample assertions pass.</returns>
    [Get("/ping")]
    Task PingAsync(CancellationToken cancellationToken);
}
