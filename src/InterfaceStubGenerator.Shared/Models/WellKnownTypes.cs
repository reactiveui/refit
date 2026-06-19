// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Resolves and caches well-known named type symbols from a compilation.</summary>
/// <param name="compilation">The compilation used to resolve type symbols.</param>
public class WellKnownTypes(Compilation compilation)
{
    /// <summary>Caches resolved type symbols by their full metadata name.</summary>
    private readonly Dictionary<string, INamedTypeSymbol?> _cachedTypes = [];

    /// <summary>Gets the named type symbol for the given type.</summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <returns>The resolved named type symbol.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "Type parameter intentionally specified explicitly by callers.")]
    public INamedTypeSymbol Get<T>() => Get(typeof(T));

    /// <summary>Gets the named type symbol for the specified type.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The resolved named type symbol.</returns>
    /// <exception cref="InvalidOperationException">Could not get name of type " + type</exception>
    public INamedTypeSymbol Get(Type type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return Get(type.FullName ?? throw new InvalidOperationException("Could not get name of type " + type));
    }

    /// <summary>Tries to resolve the named type symbol for the given full type name.</summary>
    /// <param name="typeFullName">Full name of the type.</param>
    /// <returns>The resolved named type symbol, or null if it could not be found.</returns>
    public INamedTypeSymbol? TryGet(string typeFullName)
    {
        if (_cachedTypes.TryGetValue(typeFullName, out var typeSymbol))
        {
            return typeSymbol;
        }

        typeSymbol = compilation.GetTypeByMetadataName(typeFullName);
        _cachedTypes.Add(typeFullName, typeSymbol);

        return typeSymbol;
    }

    /// <summary>Resolves a type symbol by full name, throwing if it cannot be found.</summary>
    /// <param name="typeFullName">Full name of the type.</param>
    /// <returns>The resolved named type symbol.</returns>
    private INamedTypeSymbol Get(string typeFullName) =>
        TryGet(typeFullName) ?? throw new InvalidOperationException("Could not get type " + typeFullName);
}
