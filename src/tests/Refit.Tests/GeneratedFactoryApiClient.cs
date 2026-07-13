// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A hand-written implementation of <see cref="IGeneratedFactoryApi"/> used to verify that a registered generated factory is invoked.</summary>
/// <param name="client">The HTTP client supplied by Refit.</param>
/// <param name="builder">The request builder supplied by Refit.</param>
internal sealed class GeneratedFactoryApiClient(HttpClient client, IRequestBuilder builder)
    : IGeneratedFactoryApi
{
    /// <summary>Gets the HTTP client supplied to the factory.</summary>
    public HttpClient Client { get; } = client;

    /// <summary>Gets the request builder supplied to the factory.</summary>
    public IRequestBuilder Builder { get; } = builder;

    /// <inheritdoc/>
    public Task Get() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task GetById(string id) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Search(object filter) => Task.CompletedTask;
}
