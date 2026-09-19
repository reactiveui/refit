// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Provides a small implementation for the public factory-registration examples.</summary>
/// <param name="client">The caller-owned client retained by the implementation.</param>
/// <param name="settings">The settings passed to the registered factory.</param>
internal sealed class RegisteredClient(HttpClient client, RefitSettings settings) : IRegisteredClient
{
    /// <summary>Gets the settings supplied to the registered factory.</summary>
    public RefitSettings Settings { get; } = settings;

    /// <summary>Gets the client base address supplied to the registered factory.</summary>
    public Uri? BaseAddress { get; } = client.BaseAddress;
}
