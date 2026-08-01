// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.NativeAotSmoke;

/// <summary>An interface carrying no Refit attributes, so the generator emits no client and no registration for it.</summary>
internal interface INoGeneratedClientApi
{
    /// <summary>Sends a request.</summary>
    /// <returns>The response body.</returns>
    Task<string> GetAsync();
}
