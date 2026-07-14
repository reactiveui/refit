// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.NativeAotSmoke;

/// <summary>The Refit API used by the native AOT smoke test.</summary>
public interface INativeAotApi
{
    /// <summary>Creates a to-do item.</summary>
    /// <param name="item">The item to create.</param>
    /// <returns>The created item.</returns>
    [Post("/todos")]
    Task<Todo> CreateTodoAsync([Body] Todo item);

    /// <summary>Submits URL-encoded form data.</summary>
    /// <param name="form">The form payload.</param>
    /// <returns>The form response.</returns>
    [Post("/forms")]
    Task<string> SubmitFormAsync([Body(BodySerializationMethod.UrlEncoded)] SmokeForm form);

    /// <summary>Gets the service status.</summary>
    /// <returns>The service status response.</returns>
    [Get("/status")]
    Task<ApiResponse<ServiceStatus>> GetStatusAsync();

    /// <summary>Round-trips an item through a generic method, proving generic methods generate inline (the type
    /// parameter flows through to the generated runner for both the JSON body and the result, with no reflection)
    /// and stay Native AOT clean when closed over a concrete type.</summary>
    /// <typeparam name="T">The request and response type.</typeparam>
    /// <param name="item">The item to send.</param>
    /// <returns>The round-tripped item.</returns>
    [Post("/echo")]
    Task<T> EchoAsync<T>([Body] T item);

    /// <summary>Searches with generated inline query construction covering every supported shape.</summary>
    /// <param name="q">A value requiring escaping.</param>
    /// <param name="page">A nullable page number.</param>
    /// <param name="ids">Identifiers expanded as repeated pairs.</param>
    /// <param name="sort">An enum resolved at compile time.</param>
    /// <param name="flag">A valueless query flag.</param>
    /// <param name="cursor">A caller-encoded value passed through verbatim.</param>
    /// <returns>The service status response.</returns>
    [Get("/search")]
    Task<ServiceStatus> SearchAsync(
        string q,
        int? page,
        [Query(CollectionFormat.Multi)] int[] ids,
        SmokeSort sort,
        [QueryName] string flag,
        [Encoded] string cursor);

    /// <summary>An intentionally reflection-backed method shape proving the generated fallback builds cleanly
    /// under full trimming with IL2026/IL3050 promoted to errors (reactiveui/refit#2200). It is never invoked.</summary>
    /// <returns>An observable sequence of raw responses.</returns>
    [Get("/legacy")]
    IObservable<HttpResponseMessage> ObserveLegacy();
}
