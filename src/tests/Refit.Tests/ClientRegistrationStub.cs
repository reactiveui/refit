// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A hand-written implementation returned by the factories registered in the client registration tests.</summary>
public sealed class ClientRegistrationStub
    : IGeneratedClientRegistrationApi,
    IGeneratedClientRegistrationByTypeApi,
    IInlineClientRegistrationApi,
    IInlineClientRegistrationByTypeApi
{
    /// <inheritdoc/>
    public Task<string> Get() => Task.FromResult(string.Empty);
}
