// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Refit.Generator;

/// <summary>Reserves ordinal, case-sensitive names and appends numeric suffixes to avoid collisions.</summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public class UniqueNameBuilder
{
    /// <summary>The set of names already used in this scope.</summary>
    private readonly HashSet<string> _usedNames = [with(comparer: StringComparer.Ordinal)];

    /// <summary>Reserve names.</summary>
    /// <param name="names">The name.</param>
    public void Reserve(
        IEnumerable<string> names)
    {
        if (names is null)
        {
            return;
        }

        foreach (var name in names)
        {
            _ = _usedNames.Add(name);
        }
    }

    /// <summary>Reserves a single name so it will not be handed out by <see cref="New"/>.</summary>
    /// <param name="name">The name to reserve.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reserve(string name) => _usedNames.Add(name);

    /// <summary>Generate a unique name.</summary>
    /// <param name="name">The desired base name.</param>
    /// <returns>The reserved base name, or the first unused name with a numeric suffix starting at zero.</returns>
    public string New(
        string name)
    {
        var i = 0;
        var uniqueName = name;
        while (Contains(uniqueName))
        {
            uniqueName = name + i;
            i++;
        }

        _ = _usedNames.Add(uniqueName);

        return uniqueName;
    }

    /// <summary>Reserves each name in a raw model array.</summary>
    /// <param name="names">The names to reserve.</param>
    internal void Reserve(
        string[] names)
    {
        foreach (var name in names)
        {
            _ = _usedNames.Add(name);
        }
    }

    /// <summary>Determines whether this builder has reserved the name.</summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True if the name is already used; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Contains(string name) =>
        _usedNames.Contains(name);
}
