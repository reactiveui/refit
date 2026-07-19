// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>
/// Builds unique identifier names within a nested scope hierarchy, ensuring generated members
/// do not collide with names already used in the current or any parent scope.
/// </summary>
public class UniqueNameBuilder
{
    /// <summary>The set of names already used in this scope.</summary>
    private readonly HashSet<string> _usedNames = new(StringComparer.Ordinal);

    /// <summary>Reserve names.</summary>
    /// <param name="names">The name.</param>
    public void Reserve(IEnumerable<string> names)
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
    public void Reserve(string name) => _usedNames.Add(name);

    /// <summary>Generate a unique name.</summary>
    /// <param name="name">The desired base name.</param>
    /// <returns>A unique name not used in this or any parent scope.</returns>
    public string New(string name)
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

    /// <summary>Reserves each name in a value-array, iterating its allocation-free struct enumerator.</summary>
    /// <param name="names">The names to reserve.</param>
    /// <remarks>Prefer this over the <see cref="IEnumerable{T}"/> overload for an <see cref="ImmutableEquatableArray{T}"/>:
    /// passing one as <see cref="IEnumerable{T}"/> boxes its struct enumerator, which this overload avoids.</remarks>
    internal void Reserve(ImmutableEquatableArray<string> names)
    {
        // A defaulted value-array enumerates as empty, so no null guard is needed.
        foreach (var name in names)
        {
            _ = _usedNames.Add(name);
        }
    }

    /// <summary>Determines whether the name is used in this or any parent scope.</summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True if the name is already used; otherwise, false.</returns>
    internal bool Contains(string name) =>
        _usedNames.Contains(name);
}
