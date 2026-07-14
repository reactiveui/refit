// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>A string-wrapper value whose text contains path separators, used to exercise non-string round-tripping.</summary>
/// <param name="Value">The wrapped path text (for example <c>some/repo</c>).</param>
public readonly record struct RepoPath(string Value)
{
    /// <inheritdoc/>
    public override string ToString() => Value;
}
