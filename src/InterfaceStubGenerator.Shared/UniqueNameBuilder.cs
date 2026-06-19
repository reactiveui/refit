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

    /// <summary>The parent scope, or null for a root scope.</summary>
    private readonly UniqueNameBuilder? _parentScope;

    /// <summary>Initializes a new instance of the <see cref="UniqueNameBuilder"/> class representing a root scope.</summary>
    public UniqueNameBuilder()
    {
    }

    /// <summary>Initializes a new instance of the UniqueNameBuilder class as a child scope.</summary>
    /// <param name="parentScope">The parent scope.</param>
    private UniqueNameBuilder(UniqueNameBuilder parentScope)
        : this() =>
        _parentScope = parentScope;

    /// <summary>Reserve a name.</summary>
    /// <param name="name">The name to reserve.</param>
    public void Reserve(string name) => _usedNames.Add(name);

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
            _usedNames.Add(name);
        }
    }

    /// <summary>Create a new scope.</summary>
    /// <returns>The new child scope.</returns>
    public UniqueNameBuilder NewScope() => new(this);

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

        _usedNames.Add(uniqueName);

        return uniqueName;
    }

    /// <summary>Determines whether the name is used in this or any parent scope.</summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True if the name is already used; otherwise, false.</returns>
    private bool Contains(string name)
    {
        if (_usedNames.Contains(name))
        {
            return true;
        }

        if (_parentScope is null)
        {
            return false;
        }

        return _parentScope.Contains(name);
    }
}
