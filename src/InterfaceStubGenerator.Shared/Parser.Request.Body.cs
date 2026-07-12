// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>Body and form-field parsing for the Refit source generator.</content>
internal static partial class Parser
{
    /// <summary>Builds reflection-free form field descriptors for a URL-encoded body type.</summary>
    /// <param name="bodyType">The declared body type.</param>
    /// <param name="context">The interface generation context, used to classify the scalar fast path.</param>
    /// <returns>The field descriptors, or <see langword="null"/> when the type is not eligible for the descriptor path.</returns>
    private static ImmutableEquatableArray<FormFieldModel>? TryBuildFormFields(
        ITypeSymbol bodyType,
        InterfaceGenerationContext context)
    {
        if (!IsFormFieldEligibleType(bodyType))
        {
            return null;
        }

        var fields = new List<FormFieldModel>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // bodyType is a concrete class/struct/enum here (interfaces and the like are excluded upstream), so its base
        // chain always reaches System.Object; the loop stops there and never dereferences a null BaseType.
        for (var current = bodyType;
            current.SpecialType != SpecialType.System_Object;
            current = current.BaseType!)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IPropertySymbol property && IsReadableFormProperty(property) && seen.Add(property.Name))
                {
                    fields.Add(BuildFormFieldModel(property, context));
                }
            }
        }

        return ImmutableEquatableArrayFactory.FromList(fields);
    }

    /// <summary>Determines whether a property contributes a readable public instance form field.</summary>
    /// <param name="property">The property to inspect.</param>
    /// <returns><see langword="true"/> when the property is read through a public instance getter.</returns>
    private static bool IsReadableFormProperty(IPropertySymbol property) =>
        !property.IsStatic
        && !property.IsIndexer
        && property.DeclaredAccessibility == Accessibility.Public
        && property.GetMethod is { DeclaredAccessibility: Accessibility.Public };

    /// <summary>Determines whether a body type can be flattened to form fields without reflection.</summary>
    /// <param name="type">The declared body type.</param>
    /// <returns><see langword="true"/> when descriptor-based flattening matches the reflection path.</returns>
    private static bool IsFormFieldEligibleType(ITypeSymbol type) =>
        type.TypeKind != TypeKind.Interface
        && type.TypeKind != TypeKind.TypeParameter
        && type.TypeKind != TypeKind.Dynamic
        && type.TypeKind != TypeKind.Array
        && type.TypeKind != TypeKind.Pointer
        && type.TypeKind != TypeKind.Error
        && type.SpecialType != SpecialType.System_String
        && type.SpecialType != SpecialType.System_Object
        && type is not INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T }

        // Dictionaries and other enumerables flow through the reflection path, which special-cases them.
        && !ImplementsEnumerable(type);

    /// <summary>Determines whether a type implements the non-generic <see cref="System.Collections.IEnumerable"/>.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is enumerable.</returns>
    private static bool ImplementsEnumerable(ITypeSymbol type)
    {
        // The only caller (IsFormFieldEligibleType) already excludes interface types, so type is never the
        // System.Collections.IEnumerable interface itself; a concrete enumerable always exposes it through AllInterfaces.
        foreach (var contract in type.AllInterfaces)
        {
            if (contract.SpecialType == SpecialType.System_Collections_IEnumerable)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Builds the form field descriptor for one body property.</summary>
    /// <param name="property">The property to describe.</param>
    /// <param name="context">The interface generation context, used to classify the scalar fast path.</param>
    /// <returns>The field descriptor.</returns>
    private static FormFieldModel BuildFormFieldModel(IPropertySymbol property, InterfaceGenerationContext context)
    {
        string? aliasName = null;
        string? jsonName = null;
        var query = default(QueryFormData);

        foreach (var attribute in property.GetAttributes())
        {
            var attributeName = attribute.AttributeClass!.ToDisplayString();
            if (attributeName == "Refit.AliasAsAttribute")
            {
                aliasName = GetFirstStringArgument(attribute);
            }
            else if (attributeName == "System.Text.Json.Serialization.JsonPropertyNameAttribute")
            {
                jsonName = GetFirstStringArgument(attribute);
            }
            else if (attributeName == "Refit.QueryAttribute")
            {
                query = ParseFormQueryAttribute(attribute);
            }
        }

        // A scalar property (string/bool/IFormattable, matching the query fast path) can be rendered straight-line
        // without the descriptor array; collections and complex properties leave ValueFormat null so the whole body
        // falls back to the descriptor path, which special-cases them (STJ-style: fast path the simple shapes).
        var valueFormat = IsSimpleType(property.Type, context.FormattableSymbol)
            ? BuildValueFormat(property.Type, NormalizeFormat(query.Format), context.FormattableSymbol, context)
            : null;

        var prefixSegment = string.IsNullOrWhiteSpace(query.Prefix) ? null : query.Prefix + query.Delimiter;
        return new(
            property.Name,
            aliasName ?? jsonName,
            prefixSegment,
            query.Format,
            query.CollectionFormatValue,
            query.SerializeNull,
            CanElementBeNull(property.Type),
            valueFormat);
    }

    /// <summary>Reads the first string constructor argument from an attribute.</summary>
    /// <param name="attribute">The attribute to inspect.</param>
    /// <returns>The first string argument, or <see langword="null"/>.</returns>
    private static string? GetFirstStringArgument(AttributeData attribute)
    {
        foreach (var argument in attribute.ConstructorArguments)
        {
            if (argument.Value is string value)
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>Parses the form-relevant members of a <c>[Query]</c> attribute applied to a property.</summary>
    /// <param name="attribute">The query attribute data.</param>
    /// <returns>The parsed form query data.</returns>
    private static QueryFormData ParseFormQueryAttribute(AttributeData attribute) =>
        ApplyQueryNamedArguments(attribute, ParseQueryConstructorArguments(attribute));

    /// <summary>Parses the constructor arguments of a <c>[Query]</c> attribute.</summary>
    /// <param name="attribute">The query attribute data.</param>
    /// <returns>The form query data carried by the constructor arguments.</returns>
    private static QueryFormData ParseQueryConstructorArguments(AttributeData attribute)
    {
        var delimiter = ".";
        string? prefix = null;
        string? format = null;
        int? collectionFormatValue = null;
        var stringArguments = 0;

        foreach (var argument in attribute.ConstructorArguments)
        {
            // The only enum-typed constructor argument a [Query] attribute accepts is CollectionFormat.
            if (argument.Kind == TypedConstantKind.Enum)
            {
                collectionFormatValue = (int)argument.Value!;
            }
            else if (argument.Value is string stringValue)
            {
                if (stringArguments == 0)
                {
                    delimiter = stringValue;
                }
                else if (stringArguments == 1)
                {
                    prefix = stringValue;
                }
                else
                {
                    format = stringValue;
                }

                stringArguments++;
            }
        }

        return new(delimiter, prefix, format, collectionFormatValue, false, false);
    }

    /// <summary>Applies the named arguments of a <c>[Query]</c> attribute over constructor-supplied data.</summary>
    /// <param name="attribute">The query attribute data.</param>
    /// <param name="data">The data parsed from constructor arguments.</param>
    /// <returns>The form query data with named arguments applied.</returns>
    private static QueryFormData ApplyQueryNamedArguments(AttributeData attribute, QueryFormData data)
    {
        foreach (var named in attribute.NamedArguments)
        {
            if (named.Key == "Format" && named.Value.Value is string formatValue)
            {
                data = data with { Format = formatValue };
            }
            else if (named.Key == "CollectionFormat")
            {
                data = data with { CollectionFormatValue = (int)named.Value.Value! };
            }
            else if (named.Key == "SerializeNull")
            {
                data = data with { SerializeNull = (bool)named.Value.Value! };
            }
            else if (named.Key == "TreatAsString")
            {
                data = data with { TreatAsString = (bool)named.Value.Value! };
            }
        }

        return data;
    }

    /// <summary>Parses the constructor-supplied data from a body attribute.</summary>
    /// <param name="attribute">The attribute data.</param>
    /// <returns>The parsed body serialization and buffering data.</returns>
    private static BodyAttributeInfo ParseBodyAttribute(AttributeData attribute)
    {
        var serializationMethod = "Default";
        bool? buffered = null;

        foreach (var argument in attribute.ConstructorArguments)
        {
            if (TryGetBodySerializationMethodName(argument, out var methodName))
            {
                serializationMethod = methodName;
                continue;
            }

            if (TryGetBodyBufferedValue(argument, out var boolValue))
            {
                buffered = boolValue;
            }
        }

        var bufferMode = buffered switch
        {
            true => BodyBufferMode.Buffered,
            false => BodyBufferMode.Streaming,
            _ => BodyBufferMode.Settings
        };

        return new(serializationMethod, bufferMode);
    }

    /// <summary>Tries to parse a body serialization method constructor argument.</summary>
    /// <param name="argument">The constructor argument.</param>
    /// <param name="methodName">Receives the enum member name.</param>
    /// <returns><see langword="true"/> when the argument is a body serialization method.</returns>
    private static bool TryGetBodySerializationMethodName(in TypedConstant argument, out string methodName)
    {
        // The only enum-typed constructor argument a [Body] attribute accepts is BodySerializationMethod.
        if (argument.Kind == TypedConstantKind.Enum)
        {
            methodName = GetBodySerializationMethodName((int)argument.Value!);
            return true;
        }

        methodName = string.Empty;
        return false;
    }

    /// <summary>Parsed body attribute data.</summary>
    /// <param name="SerializationMethod">The body serialization method name.</param>
    /// <param name="BufferMode">The body buffering mode.</param>
    private readonly record struct BodyAttributeInfo(
        string SerializationMethod,
        BodyBufferMode BufferMode);

    /// <summary>Form-relevant data parsed from a <c>[Query]</c> attribute on a parameter or body property.</summary>
    /// <param name="Delimiter">The delimiter combined with the prefix.</param>
    /// <param name="Prefix">The field name prefix, if any.</param>
    /// <param name="Format">The value format, if any.</param>
    /// <param name="CollectionFormatValue">The explicit collection format value, if any.</param>
    /// <param name="SerializeNull">Whether null values are serialized as empty fields.</param>
    /// <param name="TreatAsString">Whether the raw value is stringified via <c>ToString()</c> before formatting.</param>
    private readonly record struct QueryFormData(
        string Delimiter,
        string? Prefix,
        string? Format,
        int? CollectionFormatValue,
        bool SerializeNull,
        bool TreatAsString);
}
