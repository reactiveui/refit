// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Declares reusable local routes for HTTP and JSON failure checks.</summary>
internal interface IErrorsApi
{
    /// <summary>Requests a rejected response through the throwing return shape.</summary>
    /// <param name="cancellationToken">Cancellation for the request.</param>
    /// <returns>The reply model if the service accepts the request.</returns>
    [Get("/failures/rejected")]
    Task<Person> GetRejectedAsync(CancellationToken cancellationToken);

    /// <summary>Requests the same rejected response while retaining its HTTP error.</summary>
    /// <param name="cancellationToken">Cancellation for the request.</param>
    /// <returns>A disposable wrapper containing the failure status and error.</returns>
    [Get("/failures/rejected")]
    Task<ApiResponse<Person>> GetRejectedResponseAsync(CancellationToken cancellationToken);

    /// <summary>Requests a successful status with unreadable JSON content.</summary>
    /// <param name="cancellationToken">Cancellation for the request.</param>
    /// <returns>A disposable wrapper retaining the deserialization error.</returns>
    [Get("/failures/malformed")]
    Task<ApiResponse<Person>> GetMalformedAsync(CancellationToken cancellationToken);

    /// <summary>Requests a problem response containing validation entries.</summary>
    /// <param name="cancellationToken">Cancellation for the request.</param>
    /// <returns>The reply model if the service accepts the request.</returns>
    [Get("/failures/problem")]
    Task<Person> GetProblemAsync(CancellationToken cancellationToken);
}
