// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface that also implements <see cref="IDisposable"/> to verify disposable API clients.</summary>
public interface IGitHubApiDisposable : IDisposable
{
    /// <summary>Invokes an arbitrary endpoint.</summary>
    /// <returns>A task that completes when the call finishes.</returns>
    [Get("whatever")]
    Task RefitMethod();
}
