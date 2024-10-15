using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Refit.Generator
{
    static class ITypeSymbolExtensions
    {
        internal static IEnumerable<IFieldSymbol> GetFields(this ITypeSymbol symbol) => symbol.GetMembers().OfType<IFieldSymbol>();

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
            this ITypeSymbol type,
            ITypeSymbol baseType,
            bool includeInterfaces
        )
        {
            if (!includeInterfaces)
            {
                return InheritsFromOrEquals(type, baseType);
            }

            return type.GetBaseTypesAndThis()
                .Concat(type.AllInterfaces)
                .Any(t => t.Equals(baseType, SymbolEqualityComparer.Default));
        }

        // Determine if "type" inherits from "baseType", ignoring constructed types and interfaces, dealing
        // only with original types.
        public static bool InheritsFromOrEquals(this ITypeSymbol type, ITypeSymbol baseType)
        {
            return type.GetBaseTypesAndThis()
                .Any(t => t.Equals(baseType, SymbolEqualityComparer.Default));
        }

        public static IEnumerable<AttributeData> GetAttributesFor(this ISymbol type, ITypeSymbol attributeType)
        {
            return type.GetAttributes().Where(t => t.AttributeClass!.InheritsFromOrEquals(attributeType));
        }

        // TODO: most of this stuff isnt needed, I tried using this to supoort HttpMethodAttribute inheritance
        // TODO: pretty sure custom HttpMethodAttributes will break the generator, don't think I can prevent his
        public static T MapToType<T>(this AttributeData attributeData, WellKnownTypes knownTypes)
        {
            T attribute;
            var dataType = typeof(T);

            var syntax = (AttributeSyntax?)attributeData.ApplicationSyntaxReference?.GetSyntax();
            var syntaxArguments =
                (IReadOnlyList<AttributeArgumentSyntax>?)syntax?.ArgumentList?.Arguments
                ?? new AttributeArgumentSyntax[attributeData.ConstructorArguments.Length + attributeData.NamedArguments.Length];

            if (attributeData.AttributeConstructor != null && attributeData.ConstructorArguments.Length > 0)
            {
                attribute = (T) Activator.CreateInstance(typeof(T), attributeData.GetActualConstructorParams().ToArray());
            }
            else
            {
                attribute = (T) Activator.CreateInstance(typeof(T));
            }
            foreach (var p in attributeData.NamedArguments)
            {
                typeof(T).GetField(p.Key).SetValue(attribute, p.Value.Value);
            }

            var syntaxIndex = attributeData.ConstructorArguments.Length;

            var propertiesByName = dataType.GetProperties().GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First());
            foreach (var namedArgument in attributeData.NamedArguments)
            {
                if (!propertiesByName.TryGetValue(namedArgument.Key, out var prop))
                    throw new InvalidOperationException($"Could not get property {namedArgument.Key} of attribute ");

                var value = BuildArgumentValue(namedArgument.Value, prop.PropertyType, syntaxArguments[syntaxIndex], knownTypes);
                prop.SetValue(attribute, value);
                syntaxIndex++;
            }

            return attribute;
        }

        private static object? BuildArgumentValue(
            TypedConstant arg,
            Type targetType,
            AttributeArgumentSyntax? syntax,
            WellKnownTypes? knownTypes
        )
        {
            return arg.Kind switch
            {
                _ when (targetType == typeof(AttributeValue?) || targetType == typeof(AttributeValue)) && syntax != null => new AttributeValue(
                    arg,
                    syntax.Expression
                ),
                _ when arg.IsNull => null,
                TypedConstantKind.Enum => GetEnumValue(arg, targetType),
                TypedConstantKind.Array => BuildArrayValue(arg, targetType, knownTypes),
                TypedConstantKind.Primitive => arg.Value,
                TypedConstantKind.Type when targetType == typeof(ITypeSymbol) => arg.Value,
                _ => throw new ArgumentOutOfRangeException(
                    $"{nameof(WellKnownTypes)} does not support constructor arguments of kind {arg.Kind.ToString()} or cannot convert it to {targetType}"
                ),
            };
        }

        public readonly record struct AttributeValue(TypedConstant ConstantValue, ExpressionSyntax Expression);


        private static object?[] BuildArrayValue(TypedConstant arg, Type targetType, WellKnownTypes? symbolAccessor)
        {
            if (!targetType.IsGenericType || targetType.GetGenericTypeDefinition() != typeof(IReadOnlyCollection<>))
                throw new InvalidOperationException($"{nameof(IReadOnlyCollection<object>)} is the only supported array type");

            var elementTargetType = targetType.GetGenericArguments()[0];
            return arg.Values.Select(x => BuildArgumentValue(x, elementTargetType, null, symbolAccessor)).ToArray();
        }

        private static object? GetEnumValue(TypedConstant arg, Type targetType)
        {
            if (arg.Value == null)
                return null;

            var enumRoslynType = arg.Type ?? throw new InvalidOperationException("Type is null");
            if (targetType == typeof(IFieldSymbol))
                return enumRoslynType.GetFields().First(f => Equals(f.ConstantValue, arg.Value));

            if (targetType.IsConstructedGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                targetType = Nullable.GetUnderlyingType(targetType)!;
            }

            return Enum.ToObject(targetType, arg.Value);
        }


        public static IEnumerable<object> GetActualConstructorParams(this AttributeData attributeData)
        {
            foreach (var arg in attributeData.ConstructorArguments)
            {
                if (arg.Kind == TypedConstantKind.Array)
                {
                    // Assume they are strings, but the array that we get from this
                    // should actually be of type of the objects within it, be it strings or ints
                    // This is definitely possible with reflection, I just don't know how exactly.
                    yield return arg.Values.Select(a => a.Value).OfType<string>().ToArray();
                }
                else
                {
                    yield return arg.Value;
                }
            }
        }

        public static TResult? AccessFirstOrDefault<TAttribute, TResult>(this ISymbol symbol, WellKnownTypes knownTypes)
        where TAttribute : Attribute
        where TResult : class
        {
            var attributeSymbol = knownTypes.Get<TAttribute>();
            var attribute = symbol.GetAttributesFor(attributeSymbol).FirstOrDefault();
            return attribute?.MapToType<TResult>(knownTypes);
        }

        public static TResult? AccessFirstOrDefault<TResult>(this ISymbol symbol, INamedTypeSymbol attributeSymbol, WellKnownTypes knownTypes)
            where TResult : class
        {
            var attribute = symbol.GetAttributesFor(attributeSymbol).FirstOrDefault();
            return attribute?.MapToType<TResult>(knownTypes);
        }

        // public static IEnumerable<TAttribute> Access<TAttribute>(this ISymbol symbol, WellKnownTypes knownTypes)
        //     where TAttribute : Attribute
        // {
        //     var attributeSymbol = knownTypes.Get<TAttribute>();
        //     var attributes = symbol.GetAttributesFor(attributeSymbol);
        //
        //     foreach (var attribute in attributes)
        //     {
        //         yield return attribute.MapToType<TAttribute>(knownTypes);
        //     }
        // }

        public static IEnumerable<TResult> Access<TResult>(this ISymbol symbol, INamedTypeSymbol attributeSymbol, WellKnownTypes knownTypes)
        {
            var attributes = symbol.GetAttributesFor(attributeSymbol);

            foreach (var attribute in attributes)
            {
                yield return attribute.MapToType<TResult>(knownTypes);
            }
        }
    }
}
