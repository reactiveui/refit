using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Refit.Generator
{
    static class ITypeSymbolExtensions
    {
        public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol? type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        // Determine if "type" inherits from "baseType", ignoring constructed types, optionally including interfaces,
        // dealing only with original types.
        public static bool InheritsFromOrEquals(
            this ITypeSymbol type, ITypeSymbol baseType, bool includeInterfaces)
        {
            if (!includeInterfaces)
            {
                return InheritsFromOrEquals(type, baseType);
            }

            return type.GetBaseTypesAndThis().Concat(type.AllInterfaces).Any(t => t.Equals(baseType, SymbolEqualityComparer.Default));
        }


        // Determine if "type" inherits from "baseType", ignoring constructed types and interfaces, dealing
        // only with original types.
        public static bool InheritsFromOrEquals(
            this ITypeSymbol type, ITypeSymbol baseType)
        {
            return type.GetBaseTypesAndThis().Any(t => t.Equals(baseType, SymbolEqualityComparer.Default));
        }

    }
}
