// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>Flattens complex and dictionary query values and their properties into query pairs.</content>
internal static partial class Parser
{
    /// <summary>Determines how a dictionary value type renders inline: scalar, flattened sealed complex, or not at all.</summary>
    /// <param name="valueType">The dictionary value type.</param>
    /// <param name="format">The parameter-level <c>[Query(Format)]</c> applied to each value, or null.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <param name="inlineable">Receives whether the value type renders inline at all.</param>
    /// <returns>The flattened property descriptors for a sealed complex value, or null for a simple value.</returns>
    /// <remarks>
    /// A simple value renders as one <c>entryKey=value</c> pair. A sealed or value complex value (with no per-value
    /// format) flattens under each entry's key, matching the reflection builder's per-value <c>BuildQueryMap</c>
    /// recursion; because the declared type is the runtime type there is no divergence. An <c>object</c>, interface, open,
    /// or collection value keeps falling back, since the runtime value could recurse differently than the declared type.
    /// </remarks>
    private static ImmutableEquatableArray<QueryObjectPropertyModel>? ResolveDictionaryValueProperties(
        ITypeSymbol valueType,
        string? format,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context,
        out bool inlineable)
    {
        if (IsSimpleType(valueType, formattableSymbol))
        {
            inlineable = true;
            return null;
        }

        if (format is null
            && IsConcreteComplexType(valueType)
            && TryBuildQueryObjectProperties(valueType, null, formattableSymbol, context) is { } properties)
        {
            inlineable = true;
            return properties;
        }

        inlineable = false;
        return null;
    }

    /// <summary>Determines whether a type is a concrete complex type flattened or serialized by its declared shape.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> for a concrete class or value type that is neither <c>object</c> nor a collection.</returns>
    /// <remarks>
    /// Non-sealed types are accepted: the generator uses the declared shape, the same accepted divergence a non-sealed
    /// query object already carries (matching the System.Text.Json source generator). For a value that is not a runtime
    /// subtype the rendering matches the reflection builder exactly; a polymorphic subtype value renders by its declared
    /// type instead of its runtime type. An <c>object</c>, interface, or open generic type has no usable declared shape
    /// and stays on the reflection path.
    /// </remarks>
    private static bool IsConcreteComplexType(ITypeSymbol type) =>
        type.TypeKind is TypeKind.Class or TypeKind.Struct
        && type.SpecialType != SpecialType.System_Object
        && !TryGetEnumerableElementType(type, out _);

    /// <summary>Tries to resolve the key and value types of a dictionary-shaped query parameter.</summary>
    /// <param name="type">The declared parameter type.</param>
    /// <param name="keyType">Receives the dictionary key type.</param>
    /// <param name="valueType">Receives the dictionary value type.</param>
    /// <returns><see langword="true"/> when the type closes <c>IDictionary&lt;TKey, TValue&gt;</c> exactly once.</returns>
    private static bool TryGetDictionaryTypes(ITypeSymbol type, out ITypeSymbol? keyType, out ITypeSymbol? valueType)
    {
        keyType = null;
        valueType = null;

        if (IsGenericDictionaryInterface(type))
        {
            var self = (INamedTypeSymbol)type;
            keyType = self.TypeArguments[0];
            valueType = self.TypeArguments[1];
            return true;
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (!IsGenericDictionaryInterface(contract))
            {
                continue;
            }

            // A type closing IDictionary<,> more than once has an ambiguous entry shape; leave it to reflection.
            if (keyType is not null)
            {
                keyType = null;
                valueType = null;
                return false;
            }

            keyType = contract.TypeArguments[0];
            valueType = contract.TypeArguments[1];
        }

        return keyType is not null;
    }

    /// <summary>Determines whether a type is a closed <c>System.Collections.Generic.IDictionary&lt;TKey, TValue&gt;</c>.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is the generic dictionary interface.</returns>
    private static bool IsGenericDictionaryInterface(ITypeSymbol type) =>
        type is INamedTypeSymbol
        {
            TypeKind: TypeKind.Interface,
            Name: "IDictionary",
            Arity: GenericDictionaryArity,
            ContainingNamespace.Name: "Generic",
            ContainingNamespace.ContainingNamespace.Name: "Collections",
            ContainingNamespace.ContainingNamespace.ContainingNamespace.Name: "System"
        };

    /// <summary>Tries to flatten a query object's public readable properties into compile-time descriptors.</summary>
    /// <param name="type">The declared query-object type.</param>
    /// <param name="parameterPrefixSegment">The enclosing parameter's <c>prefix + delimiter</c>, or null.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The property descriptors, or null when the type must fall back to the reflection request builder.</returns>
    /// <remarks>
    /// The declared type's properties are flattened, matching how the <c>System.Text.Json</c> source generator treats a
    /// declared type. The reflection request builder instead walks the value's <em>runtime</em> type, so passing a
    /// derived instance through a base-typed parameter no longer contributes the derived type's extra properties.
    /// </remarks>
    private static ImmutableEquatableArray<QueryObjectPropertyModel>? TryBuildQueryObjectProperties(
        ITypeSymbol type,
        string? parameterPrefixSegment,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context) =>
        TryBuildQueryObjectProperties(
            type,
            parameterPrefixSegment,
            formattableSymbol,
            context,
            ImmutableHashSet<ITypeSymbol>.Empty.WithComparer(SymbolEqualityComparer.Default),
            0);

    /// <summary>Flattens a query object's properties, recursing into nested objects with cycle and depth guards.</summary>
    /// <param name="type">The declared query-object type.</param>
    /// <param name="parameterPrefixSegment">The enclosing parameter's <c>prefix + delimiter</c>, or null for nested levels.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <param name="ancestors">The types already being flattened on this path, guarding against reference cycles.</param>
    /// <param name="depth">The current nesting depth.</param>
    /// <returns>The property descriptors, or null when the type must fall back to the reflection request builder.</returns>
    private static ImmutableEquatableArray<QueryObjectPropertyModel>? TryBuildQueryObjectProperties(
        ITypeSymbol type,
        string? parameterPrefixSegment,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context,
        ImmutableHashSet<ITypeSymbol> ancestors,
        int depth)
    {
        // A cyclic type or one nested past the depth cap keeps using the reflection builder, which recurses by value.
        if (!IsInlineFlattenableQueryObject(type) || depth > MaxNestingDepth || ancestors.Contains(type))
        {
            return null;
        }

        var nextAncestors = ancestors.Add(type);
        var properties = new List<QueryObjectPropertyModel>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // type is a concrete class or struct here, so the base chain always terminates at System.Object.
        for (var current = type; current.SpecialType != SpecialType.System_Object; current = current.BaseType!)
        {
            foreach (var member in current.GetMembers())
            {
                if (!IsFlattenableProperty(member, seen, out var property))
                {
                    continue;
                }

                // A property flattens when it is a simple scalar, a collection of simple elements, or a nested concrete
                // object; any other shape (dictionary, object-typed value, or a [Query(Format)]-carrying collection)
                // falls the whole parameter back to reflection rather than emitting a partial query string.
                if (BuildQueryObjectPropertyModel(property, parameterPrefixSegment, formattableSymbol, context, nextAncestors, depth) is not { } model)
                {
                    return null;
                }

                properties.Add(model);
            }
        }

        return ImmutableEquatableArrayFactory.FromList(properties);
    }

    /// <summary>Determines whether a member is a readable, non-ignored, not-yet-seen flattenable property.</summary>
    /// <param name="member">The type member to inspect.</param>
    /// <param name="seen">The set of property names already flattened, updated when this one is accepted.</param>
    /// <param name="property">Receives the property when the member qualifies.</param>
    /// <returns><see langword="true"/> when the member is a flattenable property.</returns>
    private static bool IsFlattenableProperty(ISymbol member, HashSet<string> seen, out IPropertySymbol property)
    {
        if (member is IPropertySymbol candidate
            && IsReadableFormProperty(candidate)
            && !IsIgnoredQueryProperty(candidate)
            && seen.Add(candidate.Name))
        {
            property = candidate;
            return true;
        }

        property = null!;
        return false;
    }

    /// <summary>Determines whether a query-object type's declared properties can be flattened inline.</summary>
    /// <param name="type">The declared query-object type.</param>
    /// <returns><see langword="true"/> when the declared type exposes a statically-known property set.</returns>
    /// <remarks>
    /// <c>object</c>, interfaces and type parameters are excluded because their property set is only known once a value
    /// exists; those keep using the reflection request builder. Enumerables (including dictionaries) are excluded
    /// because the reflection builder special-cases them rather than flattening their properties.
    /// </remarks>
    private static bool IsInlineFlattenableQueryObject(ITypeSymbol type) =>
        type.TypeKind is TypeKind.Class or TypeKind.Struct
        && type.SpecialType != SpecialType.System_Object
        && type.SpecialType != SpecialType.System_String
        && type is not INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T }
        && !ImplementsEnumerable(type);

    /// <summary>Determines whether a property is excluded from query flattening by an ignore attribute.</summary>
    /// <param name="property">The property to inspect.</param>
    /// <returns><see langword="true"/> when the property carries a recognized ignore attribute.</returns>
    /// <remarks>Matches the reflection builder, which compares attribute full names so any assembly's copy counts.</remarks>
    private static bool IsIgnoredQueryProperty(IPropertySymbol property)
    {
        foreach (var attribute in property.GetAttributes())
        {
            var attributeName = attribute.AttributeClass!.ToDisplayString();
            if (attributeName is "System.Runtime.Serialization.IgnoreDataMemberAttribute"
                or "System.Text.Json.Serialization.JsonIgnoreAttribute"
                or "Newtonsoft.Json.JsonIgnoreAttribute")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Reads the <c>[AliasAs]</c>, <c>[JsonPropertyName]</c> and <c>[Query]</c> data from a property.</summary>
    /// <param name="property">The property to inspect.</param>
    /// <returns>The alias name, the System.Text.Json name, and the parsed query data.</returns>
    private static (string? Alias, string? Json, QueryFormData Query) ReadQueryPropertyAttributes(IPropertySymbol property)
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

        return (aliasName, jsonName, query);
    }

    /// <summary>Builds the descriptor for one flattened query-object property, or null when it cannot flatten inline.</summary>
    /// <param name="property">The property to describe.</param>
    /// <param name="parameterPrefixSegment">The enclosing parameter's <c>prefix + delimiter</c>, or null.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <param name="ancestors">The types already being flattened on this path, guarding against reference cycles.</param>
    /// <param name="depth">The current nesting depth.</param>
    /// <returns>The property descriptor, or null when the property's shape must fall back to reflection.</returns>
    private static QueryObjectPropertyModel? BuildQueryObjectPropertyModel(
        IPropertySymbol property,
        string? parameterPrefixSegment,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context,
        ImmutableHashSet<ITypeSymbol> ancestors,
        int depth)
    {
        var (aliasName, jsonName, query) = ReadQueryPropertyAttributes(property);

        var propertyPrefixSegment = string.IsNullOrWhiteSpace(query.Prefix) ? null : query.Prefix + query.Delimiter;
        var prefixSegment = parameterPrefixSegment + propertyPrefixSegment;
        var normalizedPrefix = string.IsNullOrEmpty(prefixSegment) ? null : prefixSegment;
        var propertyFormat = NormalizeFormat(query.Format);

        // [AliasAs] always wins and bypasses the key formatter; the System.Text.Json field name is honored only when
        // RefitSettings.HonorContentSerializerPropertyNamesInQuery is set, so it is carried as a separate name.
        var serializerName = aliasName is null ? jsonName : null;

        // A simple property flattens to one scalar pair. A [Query(Format)] on a non-simple (complex or collection)
        // property also renders the whole value as a single pair, not a flattened or expanded one: the reflection
        // builder's TryFormatQueryPropertyValue stringifies the value through the form formatter before any enumerable
        // or nested branch. That is exactly the scalar model - the value format is ToString-only for a non-IFormattable
        // type (matching string.Format("{0:format}", value)), and the emitter still routes to
        // FormUrlEncodedParameterFormatter.Format when the formatter is customized.
        if (IsSimpleType(property.Type, formattableSymbol) || propertyFormat is not null)
        {
            return new(
                property.Name,
                aliasName,
                serializerName,
                normalizedPrefix,
                query.SerializeNull,
                CanElementBeNull(property.Type),
                propertyFormat,
                BuildValueFormat(property.Type, propertyFormat, formattableSymbol, context));
        }

        if (TryBuildCollectionPropertyModel(property, aliasName, serializerName, normalizedPrefix, query, formattableSymbol, context) is { } collectionModel)
        {
            return collectionModel;
        }

        // A dictionary property of simple keys and values expands its entries under this property's key, one
        // key.entryKey=value pair per entry, exactly as the reflection builder's nested BuildQueryMap does.
        if (TryBuildDictionaryPropertyModel(property, aliasName, serializerName, normalizedPrefix, query, formattableSymbol, context) is { } dictionaryModel)
        {
            return dictionaryModel;
        }

        // A nested concrete object flattens recursively. Its children carry no parameter prefix (this property's key
        // already includes it); they compose their keys under this property's key with the parameter delimiter.
        // A nullable value type (Nullable<T>) is unwrapped so its underlying struct flattens like a nullable class,
        // accessed through .Value after the null check the emitter already emits for a nullable nested property.
        var nestedType = property.Type;
        var nestedThroughValue = false;
        if (property.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            // A nested Nullable<T> is always a constructed named type, so its single type argument is the struct.
            nestedType = ((INamedTypeSymbol)property.Type).TypeArguments[0];
            nestedThroughValue = true;
        }

        return TryBuildQueryObjectProperties(nestedType, null, formattableSymbol, context, ancestors, depth + 1) is { } nested
            ? new(
                property.Name,
                aliasName,
                serializerName,
                normalizedPrefix,
                query.SerializeNull,
                CanElementBeNull(property.Type),
                null,
                BuildValueFormat(property.Type, null, formattableSymbol, context),
                Nested: nested,
                NestedThroughValue: nestedThroughValue)
            : null;
    }

    /// <summary>Builds the descriptor for a dictionary property of simple keys and values, or null for any other shape.</summary>
    /// <param name="property">The property to describe.</param>
    /// <param name="aliasName">The resolved <c>[AliasAs]</c> name, or null.</param>
    /// <param name="serializerName">The resolved content-serializer name, or null.</param>
    /// <param name="normalizedPrefix">The resolved key prefix, or null.</param>
    /// <param name="query">The property's parsed <c>[Query]</c> data.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The dictionary property descriptor, or null when the property is not a simple-keyed and -valued dictionary.</returns>
    private static QueryObjectPropertyModel? TryBuildDictionaryPropertyModel(
        IPropertySymbol property,
        string? aliasName,
        string? serializerName,
        string? normalizedPrefix,
        in QueryFormData query,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context) =>
        !TryGetDictionaryTypes(property.Type, out var keyType, out var valueType)
        || !IsSimpleType(keyType!, formattableSymbol)
        || !IsSimpleType(valueType!, formattableSymbol)
            ? null
            : new(
                property.Name,
                aliasName,
                serializerName,
                normalizedPrefix,
                query.SerializeNull,
                CanElementBeNull(property.Type),
                null,
                BuildValueFormat(valueType!, null, formattableSymbol, context),
                Dictionary: new(
                    QualifyType(keyType!, context),
                    BuildValueFormat(keyType!, null, formattableSymbol, context),
                    CanElementBeNull(valueType!),
                    PrefixSegment: null));

    /// <summary>Builds the descriptor for a collection-of-simple-elements property, or null for any other shape.</summary>
    /// <param name="property">The property to describe.</param>
    /// <param name="aliasName">The resolved <c>[AliasAs]</c> name, or null.</param>
    /// <param name="serializerName">The resolved content-serializer name, or null.</param>
    /// <param name="normalizedPrefix">The resolved key prefix, or null.</param>
    /// <param name="query">The property's parsed <c>[Query]</c> data.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The collection property descriptor, or null when the property is not a collection of simple elements.</returns>
    private static QueryObjectPropertyModel? TryBuildCollectionPropertyModel(
        IPropertySymbol property,
        string? aliasName,
        string? serializerName,
        string? normalizedPrefix,
        in QueryFormData query,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context)
    {
        if (!TryGetEnumerableElementType(property.Type, out var elementType)
            || !IsSimpleType(elementType!, formattableSymbol))
        {
            return null;
        }

        var collection = new QueryObjectCollectionModel(
            query.CollectionFormatValue,
            CanElementBeNull(elementType!),
            QualifyType(property.Type, context));

        return new(
            property.Name,
            aliasName,
            serializerName,
            normalizedPrefix,
            query.SerializeNull,
            CanElementBeNull(property.Type),
            null,
            BuildValueFormat(elementType!, null, formattableSymbol, context),
            collection);
    }
}
