// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>An interface carrying no Refit attributes so the generator never registers a client for it, leaving the
/// registration timing under the test's control.</summary>
public interface IInlineClientRegistrationApi
{
    /// <summary>Sends a request.</summary>
    /// <returns>The response body.</returns>
    Task<string> Get();
}
