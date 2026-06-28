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
    
    /// <summary>Gets the service status.</summary>
    /// <returns>The service status response.</returns>
    [Get("/status/{id}")]
    Task<ApiResponse<ServiceStatus>> GettererStatusAsync(int id);
    
    /// <summary>Gets the service status.</summary>
    /// <returns>The service status response.</returns>
    [Get("/status/{doer.id}")]
    Task<ApiResponse<ServiceStatus>> ObjectStatusAsync(Doer doer);

    public record Doer(int id);
}
