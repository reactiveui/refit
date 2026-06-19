// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit;

/// <summary>An <see cref="IDisposable"/> that runs the supplied action when disposed.</summary>
/// <param name="block">The action to run on disposal.</param>
internal sealed class AnonymousDisposable(Action block) : IDisposable
{
    /// <inheritdoc/>
    public void Dispose() => block();
}
