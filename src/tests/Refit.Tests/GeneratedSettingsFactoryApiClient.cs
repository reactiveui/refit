// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A hand-written implementation of <see cref="IGeneratedSettingsFactoryApi"/> used to verify generated settings factories.</summary>
/// <param name="client">The HTTP client supplied by Refit.</param>
/// <param name="settings">The settings supplied by Refit.</param>
internal sealed class GeneratedSettingsFactoryApiClient(
    HttpClient client,
    RefitSettings settings) : IGeneratedSettingsFactoryApi
{
    /// <summary>Gets the HTTP client supplied to the factory.</summary>
    internal HttpClient Client { get; } = client;

    /// <summary>Gets the settings supplied to the factory.</summary>
    internal RefitSettings Settings { get; } = settings;

    /// <inheritdoc/>
    public Task Get() => Task.CompletedTask;
}
