// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Extension methods for working with <see cref="ITypeSymbol"/> instances.</summary>
internal static class ITypeSymbolExtensions
{
    /// <summary>Extensions for a non-nullable <see cref="ITypeSymbol"/> receiver.</summary>
    /// <param name="type">The type symbol to operate on.</param>
    extension(ITypeSymbol type)
    {
        /// <summary>Determines whether the type inherits from or equals the base type, optionally including interfaces.</summary>
        /// <param name="baseType">The base type to look for.</param>
        /// <param name="includeInterfaces">True to also consider implemented interfaces.</param>
        /// <returns>True if the type inherits from or equals the base type.</returns>
        public bool InheritsFromOrEquals(ITypeSymbol baseType, bool includeInterfaces)
        {
            if (!includeInterfaces)
            {
                return type.InheritsFromOrEquals(baseType);
            }

            return type.GetBaseTypesAndThis()
                .Concat(type.AllInterfaces)
                .Any(t => t.Equals(baseType, SymbolEqualityComparer.Default));
        }

        /// <summary>Determines whether the type inherits from or equals the base type, ignoring interfaces.</summary>
        /// <param name="baseType">The base type to look for.</param>
        /// <returns>True if the type inherits from or equals the base type.</returns>
        public bool InheritsFromOrEquals(ITypeSymbol baseType) =>
            type.GetBaseTypesAndThis()
                .Any(t => t.Equals(baseType, SymbolEqualityComparer.Default));
    }

    /// <summary>Extensions for a nullable <see cref="ITypeSymbol"/> receiver.</summary>
    /// <param name="type">The nullable type symbol to operate on.</param>
    extension(ITypeSymbol? type)
    {
        /// <summary>Enumerates the type itself followed by each of its base types.</summary>
        /// <returns>The type and its base types.</returns>
        public IEnumerable<ITypeSymbol> GetBaseTypesAndThis()
        {
            var current = type;
            while (current is not null)
            {
                yield return current;
                current = current.BaseType;
            }
        }
    }
}
