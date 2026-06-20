// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>API surface with a method missing a Refit HTTP method attribute.</summary>
public interface IMissingHttpMethodApi
{
    /// <summary>Method fixture with no Refit HTTP method attribute.</summary>
    void MethodWithoutHttpMethod();
}
