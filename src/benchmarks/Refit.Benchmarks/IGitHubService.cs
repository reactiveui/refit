// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Benchmarks;

/// <summary>A benchmark Refit service exercising every supported return type.</summary>
public interface IGitHubService
{
    // Task - throws
    /// <summary>Gets users and returns a non-generic task.</summary>
    /// <returns>A task that completes when the request has finished.</returns>
    [Get("/users")]
    public Task GetUsersTaskAsync();

    /// <summary>Posts users and returns a non-generic task.</summary>
    /// <param name="users">The users to post.</param>
    /// <returns>A task that completes when the request has finished.</returns>
    [Post("/users")]
    public Task PostUsersTaskAsync([Body] IEnumerable<User> users);

    // Task<string> - throws
    /// <summary>Gets users as a raw string response.</summary>
    /// <returns>The response body as a string.</returns>
    [Get("/users")]
    public Task<string> GetUsersTaskStringAsync();

    /// <summary>Posts users and returns a raw string response.</summary>
    /// <param name="users">The users to post.</param>
    /// <returns>The response body as a string.</returns>
    [Post("/users")]
    public Task<string> PostUsersTaskStringAsync([Body] IEnumerable<User> users);

    // Task<Stream> - throws
    /// <summary>Gets users as a response stream.</summary>
    /// <returns>The response body as a stream.</returns>
    [Get("/users")]
    public Task<Stream> GetUsersTaskStreamAsync();

    /// <summary>Posts users and returns a response stream.</summary>
    /// <param name="users">The users to post.</param>
    /// <returns>The response body as a stream.</returns>
    [Post("/users")]
    public Task<Stream> PostUsersTaskStreamAsync([Body] IEnumerable<User> users);

    // Task<HttpContent> - throws
    /// <summary>Gets users as raw HTTP content.</summary>
    /// <returns>The response HTTP content.</returns>
    [Get("/users")]
    public Task<HttpContent> GetUsersTaskHttpContentAsync();

    /// <summary>Posts users and returns raw HTTP content.</summary>
    /// <param name="users">The users to post.</param>
    /// <returns>The response HTTP content.</returns>
    [Post("/users")]
    public Task<HttpContent> PostUsersTaskHttpContentAsync([Body] IEnumerable<User> users);

    // Task<HttpResponseMessage>
    /// <summary>Gets users as a full HTTP response message.</summary>
    /// <returns>The HTTP response message.</returns>
    [Get("/users")]
    public Task<HttpResponseMessage> GetUsersTaskHttpResponseMessageAsync();

    /// <summary>Posts users and returns a full HTTP response message.</summary>
    /// <param name="users">The users to post.</param>
    /// <returns>The HTTP response message.</returns>
    [Post("/users")]
    public Task<HttpResponseMessage> PostUsersTaskHttpResponseMessageAsync(
        [Body] IEnumerable<User> users);

    // IObservable<HttpResponseMessage>
    /// <summary>Gets users as an observable HTTP response message.</summary>
    /// <returns>An observable producing the HTTP response message.</returns>
    [Get("/users")]
    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Refit interface method describing an HTTP GET endpoint; it cannot be a property.")]
    public IObservable<HttpResponseMessage> GetUsersObservableHttpResponseMessage();

    /// <summary>Posts users and returns an observable HTTP response message.</summary>
    /// <param name="users">The users to post.</param>
    /// <returns>An observable producing the HTTP response message.</returns>
    [Post("/users")]
    public IObservable<HttpResponseMessage> PostUsersObservableHttpResponseMessage(
        [Body] IEnumerable<User> users);

    // Task<<T>> - throws
    /// <summary>Gets users deserialized into a list.</summary>
    /// <returns>The list of users.</returns>
    [Get("/users")]
    public Task<List<User>> GetUsersTaskTAsync();

    /// <summary>Posts users and returns the deserialized list.</summary>
    /// <param name="users">The users to post.</param>
    /// <returns>The list of users.</returns>
    [Post("/users")]
    public Task<List<User>> PostUsersTaskTAsync([Body] IEnumerable<User> users);

    // Task<ApiResponse<T>>
    /// <summary>Gets users wrapped in an API response.</summary>
    /// <returns>The API response containing the list of users.</returns>
    [Get("/users")]
    public Task<ApiResponse<List<User>>> GetUsersTaskApiResponseTAsync();

    /// <summary>Posts users and returns them wrapped in an API response.</summary>
    /// <param name="users">The users to post.</param>
    /// <returns>The API response containing the list of users.</returns>
    [Post("/users")]
    public Task<ApiResponse<List<User>>> PostUsersTaskApiResponseTAsync(
        [Body] IEnumerable<User> users);
}
