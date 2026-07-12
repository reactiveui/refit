// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>Query parameter classification for generated request construction.</content>
internal static partial class Parser
{
    /// <summary>The number of type arguments on <c>IDictionary&lt;TKey, TValue&gt;</c>.</summary>
    private const int GenericDictionaryArity = 2;

    /// <summary>The maximum nested-object depth flattened inline before the whole parameter falls back to reflection.</summary>
    private const int MaxNestingDepth = 32;

    /// <summary>The metadata name of <c>Refit.QueryAttribute</c>.</summary>
    private const string QueryAttributeDisplayName = "QueryAttribute";

    /// <summary>The metadata name of <c>Refit.QueryNameAttribute</c>.</summary>
    private const string QueryNameAttributeDisplayName = "QueryNameAttribute";

    /// <summary>The metadata name of <c>Refit.QueryConverterAttribute</c>.</summary>
    private const string QueryConverterAttributeDisplayName = "QueryConverterAttribute";

    /// <summary>The metadata name of <c>Refit.EncodedAttribute</c>.</summary>
    private const string EncodedAttributeDisplayName = "EncodedAttribute";

    /// <summary>The metadata name of <c>Refit.AuthorizeAttribute</c>.</summary>
    private const string AuthorizeAttributeDisplayName = "AuthorizeAttribute";

    /// <summary>The default authorization scheme when <c>[Authorize]</c> is used without an explicit scheme.</summary>
    private const string DefaultAuthorizeScheme = "Bearer";

    /// <summary>The metadata name of <c>Refit.BodyAttribute</c>.</summary>
    private const string BodyAttributeDisplayName = "BodyAttribute";

    /// <summary>The metadata name of <c>Refit.HeaderAttribute</c>.</summary>
    private const string HeaderAttributeDisplayName = "HeaderAttribute";

    /// <summary>The metadata name of <c>Refit.HeaderCollectionAttribute</c>.</summary>
    private const string HeaderCollectionAttributeDisplayName = "HeaderCollectionAttribute";

    /// <summary>The metadata name of <c>Refit.PropertyAttribute</c>.</summary>
    private const string PropertyAttributeDisplayName = "PropertyAttribute";

    /// <summary>The metadata name of <c>Refit.AliasAsAttribute</c>.</summary>
    private const string AliasAsAttributeDisplayName = "AliasAsAttribute";

    /// <summary>The metadata name of <c>Refit.HeadersAttribute</c>.</summary>
    private const string HeadersAttributeDisplayName = "HeadersAttribute";

    /// <summary>The metadata name of <c>Refit.MultipartAttribute</c>.</summary>
    private const string MultipartAttributeDisplayName = "MultipartAttribute";

    /// <summary>The metadata name of <c>Refit.QueryUriFormatAttribute</c>.</summary>
    private const string QueryUriFormatAttributeDisplayName = "QueryUriFormatAttribute";

    /// <summary>The fully-qualified display name of the <c>EnumMember</c> attribute honored by the default formatter.</summary>
    private const string EnumMemberAttributeDisplayName = "System.Runtime.Serialization.EnumMemberAttribute";

    /// <summary>Determines whether an attribute class is the named Refit attribute, without allocating display strings.</summary>
    /// <param name="attributeClass">The attribute class symbol.</param>
    /// <param name="attributeMetadataName">The attribute's metadata name inside the <c>Refit</c> namespace.</param>
    /// <returns><see langword="true"/> when the attribute matches.</returns>
    private static bool IsRefitAttribute(INamedTypeSymbol? attributeClass, string attributeMetadataName) =>
        attributeClass is not null
        && attributeClass.Name == attributeMetadataName
        && attributeClass.ContainingNamespace is { Name: "Refit", ContainingNamespace.IsGlobalNamespace: true };

    /// <summary>Finds a parameter attribute by its Refit metadata name.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <param name="attributeMetadataName">The attribute's metadata name inside the <c>Refit</c> namespace.</param>
    /// <returns>The attribute data, or null when absent.</returns>
    private static AttributeData? FindParameterAttribute(IParameterSymbol parameter, string attributeMetadataName)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (IsRefitAttribute(attribute.AttributeClass, attributeMetadataName))
            {
                return attribute;
            }
        }

        return null;
    }

    /// <summary>Determines whether a parameter carries the named Refit attribute.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <param name="attributeMetadataName">The attribute's metadata name inside the <c>Refit</c> namespace.</param>
    /// <returns><see langword="true"/> when the attribute is present.</returns>
    private static bool HasParameterAttribute(IParameterSymbol parameter, string attributeMetadataName) =>
        FindParameterAttribute(parameter, attributeMetadataName) is not null;

    /// <summary>Determines whether any placeholder binds a property of this parameter (a dotted <c>{param.Prop}</c>).</summary>
    /// <param name="parameterLocations">The placeholder names in the URL template.</param>
    /// <param name="urlName">The parameter's resolved URL name.</param>
    /// <returns><see langword="true"/> when a dotted placeholder targets this parameter.</returns>
    private static bool HasDottedPlaceholderFor(
        Dictionary<string, List<Range>> parameterLocations,
        string urlName)
    {
        foreach (var key in parameterLocations.Keys)
        {
            if (key.Length > urlName.Length
                && key[urlName.Length] == '.'
                && key.StartsWith(urlName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Tries to build the query-binding model for a <c>[Query]</c> or auto-appended query parameter.</summary>
    /// <param name="parameter">The parameter to classify.</param>
    /// <param name="urlName">The resolved query key.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <param name="query">Receives the query-binding model.</param>
    /// <returns><see langword="true"/> when the parameter's shape is supported by inline query generation.</returns>
    private static bool TryBuildQueryModel(
        IParameterSymbol parameter,
        string urlName,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context,
        out QueryParameterModel? query)
    {
        var data = ParseParameterQueryData(parameter);
        var preEncoded = HasParameterAttribute(parameter, EncodedAttributeDisplayName);

        // A [QueryConverter] hands flattening to a user-supplied IQueryConverter<T>, so any parameter shape — including
        // ones the declared-type walk cannot flatten — generates inline by delegating to it.
        if (FindParameterAttribute(parameter, QueryConverterAttributeDisplayName) is { } converterAttribute)
        {
            query = BuildConverterQueryModel(parameter, converterAttribute, data, preEncoded, formattableSymbol, context);
            return query is not null;
        }

        // TreatAsString, or an explicitly empty Format, stringifies the raw value via ToString() before the
        // formatter runs, mirroring the reflection request builder. This shape supports any parameter type.
        if (data.TreatAsString || data.Format is { Length: 0 })
        {
            query = new(
                urlName,
                QueryParameterShape.Scalar,
                TreatAsString: true,
                preEncoded,
                data.CollectionFormatValue,
                ElementCanBeNull: false,
                BuildValueFormat(parameter.Type, null, formattableSymbol, context));
            return true;
        }

        var format = NormalizeFormat(data.Format);
        if (IsSimpleType(parameter.Type, formattableSymbol))
        {
            query = new(
                urlName,
                QueryParameterShape.Scalar,
                TreatAsString: false,
                preEncoded,
                data.CollectionFormatValue,
                ElementCanBeNull: false,
                BuildValueFormat(parameter.Type, format, formattableSymbol, context));
            return true;
        }

        query = TryBuildScalarCollectionModel(parameter.Type, urlName, preEncoded, data, format, formattableSymbol, context);
        if (query is not null)
        {
            return true;
        }

        // A complex object is flattened into one query pair per public readable property, mirroring the reflection
        // builder's BuildQueryMap. The enclosing [Query(Prefix)] segment is folded into each property's key.
        var parameterPrefixSegment = string.IsNullOrWhiteSpace(data.Prefix) ? null : data.Prefix + data.Delimiter;

        query = TryBuildDictionaryQueryModel(parameter.Type, urlName, preEncoded, format, parameterPrefixSegment, formattableSymbol, context);
        if (query is not null)
        {
            return true;
        }

        if (TryBuildQueryObjectProperties(parameter.Type, parameterPrefixSegment, formattableSymbol, context) is { } properties)
        {
            query = new(
                urlName,
                QueryParameterShape.Object,
                TreatAsString: false,
                preEncoded,
                data.CollectionFormatValue,
                ElementCanBeNull: false,
                BuildValueFormat(parameter.Type, format, formattableSymbol, context),
                properties,
                NestingDelimiter: string.IsNullOrEmpty(data.Delimiter) ? "." : data.Delimiter);
            return true;
        }

        query = null;
        return false;
    }

    /// <summary>Builds the query model for a collection-of-simple-elements parameter, or null for any other shape.</summary>
    /// <param name="type">The declared parameter type.</param>
    /// <param name="urlName">The resolved query key.</param>
    /// <param name="preEncoded">Whether the parameter carries <c>[Encoded]</c>.</param>
    /// <param name="data">The parameter's parsed <c>[Query]</c> data.</param>
    /// <param name="format">The effective compile-time format, or null.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The collection query model, or null when the parameter is not a collection of simple elements.</returns>
    private static QueryParameterModel? TryBuildScalarCollectionModel(
        ITypeSymbol type,
        string urlName,
        bool preEncoded,
        QueryFormData data,
        string? format,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context) =>
        TryGetEnumerableElementType(type, out var elementType) && IsSimpleType(elementType!, formattableSymbol)
            ? new(
                urlName,
                QueryParameterShape.Collection,
                TreatAsString: false,
                preEncoded,
                data.CollectionFormatValue,
                CanElementBeNull(elementType!),
                BuildValueFormat(elementType!, format, formattableSymbol, context))
            : null;

    /// <summary>Builds the query model for a <c>[QueryConverter]</c> parameter, or null when the type is unresolved.</summary>
    /// <param name="parameter">The parameter carrying the converter.</param>
    /// <param name="converterAttribute">The <c>[QueryConverter]</c> attribute.</param>
    /// <param name="data">The parameter's parsed <c>[Query]</c> data supplying any prefix.</param>
    /// <param name="preEncoded">Whether the parameter carries <c>[Encoded]</c>.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The converter query model, or null when the converter type cannot be resolved.</returns>
    private static QueryParameterModel? BuildConverterQueryModel(
        IParameterSymbol parameter,
        AttributeData converterAttribute,
        QueryFormData data,
        bool preEncoded,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context)
    {
        // The converter type is the sole typeof(...) constructor argument.
        if (converterAttribute.ConstructorArguments.IsEmpty
            || converterAttribute.ConstructorArguments[0].Value is not ITypeSymbol converterType)
        {
            return null;
        }

        var keyPrefix = string.IsNullOrWhiteSpace(data.Prefix) ? string.Empty : data.Prefix + data.Delimiter;
        return new(
            string.Empty,
            QueryParameterShape.Converter,
            TreatAsString: false,
            preEncoded,
            CollectionFormatValue: null,
            ElementCanBeNull: false,
            BuildValueFormat(parameter.Type, null, formattableSymbol, context),
            Converter: new(
                QualifyType(converterType, context),
                keyPrefix));
    }

    /// <summary>Tries to build the query-binding model for a dictionary-shaped query parameter.</summary>
    /// <param name="type">The declared parameter type.</param>
    /// <param name="urlName">The resolved query key, unused because each entry supplies its own key.</param>
    /// <param name="preEncoded">Whether values pass through verbatim.</param>
    /// <param name="format">The parameter-level <c>[Query(Format)]</c> applied to each value.</param>
    /// <param name="parameterPrefixSegment">The parameter's compile-time <c>prefix + delimiter</c>, or null.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The dictionary query model, or null when the shape must fall back to the reflection request builder.</returns>
    /// <remarks>
    /// Only simple keys and values generate inline: the reflection builder inspects each value's <em>runtime</em> type
    /// to decide whether to recurse into it, which a declared-type walk cannot reproduce for <c>object</c> values.
    /// </remarks>
    private static QueryParameterModel? TryBuildDictionaryQueryModel(
        ITypeSymbol type,
        string urlName,
        bool preEncoded,
        string? format,
        string? parameterPrefixSegment,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context) =>
        !TryGetDictionaryTypes(type, out var keyType, out var valueType)
        || !IsSimpleType(keyType!, formattableSymbol)
        || !IsSimpleType(valueType!, formattableSymbol)
            ? null
            : new(
                urlName,
                QueryParameterShape.Dictionary,
                TreatAsString: false,
                preEncoded,
                CollectionFormatValue: null,
                ElementCanBeNull: false,
                BuildValueFormat(valueType!, format, formattableSymbol, context),
                Dictionary: new(
                    QualifyType(keyType!, context),
                    BuildValueFormat(keyType!, null, formattableSymbol, context),
                    CanElementBeNull(valueType!),
                    parameterPrefixSegment));

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
            var attributeName = attribute.AttributeClass?.ToDisplayString();
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

        if (IsSimpleType(property.Type, formattableSymbol))
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

        // A [Query(Format)] on a non-simple property is rejected: the reflection builder stringifies the whole value
        // through the form formatter instead of flattening or formatting elements, so it keeps using reflection.
        if (propertyFormat is not null)
        {
            return null;
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
        if (property.Type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableValueType)
        {
            nestedType = nullableValueType.TypeArguments[0];
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
        QueryFormData query,
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
        QueryFormData query,
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

    /// <summary>Builds the query-binding model for a <c>[QueryName]</c> valueless flag parameter.</summary>
    /// <param name="parameter">The parameter to classify.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The flag query model, or null when the shape is not supported inline.</returns>
    private static QueryParameterModel? TryBuildFlagModel(
        IParameterSymbol parameter,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context)
    {
        var data = ParseParameterQueryData(parameter);
        var preEncoded = HasParameterAttribute(parameter, EncodedAttributeDisplayName);
        var format = NormalizeFormat(data.Format);

        // Strings and other scalar-rendering values become one flag; enumerables render one flag per element.
        return parameter.Type.SpecialType != SpecialType.System_String
            && TryGetEnumerableElementType(parameter.Type, out var elementType)
            ? new(
                string.Empty,
                QueryParameterShape.FlagCollection,
                TreatAsString: false,
                preEncoded,
                CollectionFormatValue: null,
                CanElementBeNull(elementType!),
                BuildValueFormat(elementType!, format, formattableSymbol, context))
            : new QueryParameterModel(
                string.Empty,
                QueryParameterShape.Flag,
                TreatAsString: false,
                preEncoded,
                CollectionFormatValue: null,
                ElementCanBeNull: false,
                BuildValueFormat(parameter.Type, format, formattableSymbol, context));
    }

    /// <summary>Normalizes a compile-time format string, mapping empty/whitespace to no format.</summary>
    /// <param name="format">The raw format from the query attribute.</param>
    /// <returns>The effective format, or null.</returns>
    private static string? NormalizeFormat(string? format) =>
        string.IsNullOrWhiteSpace(format) ? null : format;

    /// <summary>Builds the reflection-free rendering strategy for one statically-known value type.</summary>
    /// <param name="type">The declared value type.</param>
    /// <param name="format">The effective compile-time format, or null.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The rendering strategy matching the default URL parameter formatter's output.</returns>
    private static InlineValueFormatModel BuildValueFormat(
        ITypeSymbol type,
        string? format,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context)
    {
        var isNullableValueType = false;
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            isNullableValueType = true;
            type = nullable.TypeArguments[0];
        }

        var typeName = QualifyType(type, context);

        if (type.SpecialType == SpecialType.System_String)
        {
            return new(InlineFormatKind.String, format, typeName, isNullableValueType, null);
        }

        // bool and char are scalars without IFormattable; string.Format ignores any format spec for them.
        if (type.SpecialType is SpecialType.System_Boolean or SpecialType.System_Char)
        {
            return new(InlineFormatKind.ToStringOnly, format, typeName, isNullableValueType, null);
        }

        if (type.TypeKind == TypeKind.Enum)
        {
            var members = BuildEnumFormatMembers(type);
            return members is null
                ? new(InlineFormatKind.FormatterOnly, format, typeName, isNullableValueType, null)
                : new(InlineFormatKind.Enum, format, typeName, isNullableValueType, members);
        }

        // One pass over AllInterfaces resolves both the IFormattable and ISpanFormattable questions, instead of
        // walking the interface list again inside ComputeSpanFormattableTiers.
        var (implementsFormattable, implementsSpanFormattable) =
            ClassifyFormattable(type, formattableSymbol, context.SpanFormattableSymbol);
        if (!implementsFormattable)
        {
            return new(InlineFormatKind.ToStringOnly, format, typeName, isNullableValueType, null);
        }

        var (urlSafe, escapable) = ComputeSpanFormattableTiers(
            type,
            format,
            isNullableValueType,
            implementsSpanFormattable,
            context);
        return new(InlineFormatKind.Formattable, format, typeName, isNullableValueType, null)
        {
            IsUrlSafeSpanFormattable = urlSafe,
            IsSpanFormattableEscapable = escapable,
        };
    }

    /// <summary>Determines, in a single interface walk, whether a type implements <c>IFormattable</c> and <c>ISpanFormattable</c>.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="spanFormattableSymbol">The resolved <c>System.ISpanFormattable</c> symbol, or null when unavailable.</param>
    /// <returns>Whether the type implements each interface.</returns>
    private static (bool Formattable, bool SpanFormattable) ClassifyFormattable(
        ITypeSymbol type,
        INamedTypeSymbol? formattableSymbol,
        INamedTypeSymbol? spanFormattableSymbol)
    {
        var formattable = false;
        var spanFormattable = false;
        foreach (var implemented in type.AllInterfaces)
        {
            if (!formattable && SymbolEqualityComparer.Default.Equals(implemented, formattableSymbol))
            {
                formattable = true;
            }

            if (!spanFormattable
                && spanFormattableSymbol is not null
                && SymbolEqualityComparer.Default.Equals(implemented, spanFormattableSymbol))
            {
                spanFormattable = true;
            }

            if (formattable && spanFormattable)
            {
                break;
            }
        }

        return (formattable, spanFormattable);
    }

    /// <summary>Computes the two path fast-write tiers a formattable value type supports on the consumer target.</summary>
    /// <param name="type">The unwrapped value type.</param>
    /// <param name="format">The compile-time format, or null.</param>
    /// <param name="isNullableValueType">Whether the source value is a nullable value type.</param>
    /// <param name="implementsSpanFormattable">Whether the value type implements <c>ISpanFormattable</c>, resolved in the single interface walk.</param>
    /// <param name="context">The generation context carrying the resolved fast-path capabilities.</param>
    /// <returns>Whether the value qualifies for the net6+ URL-safe integer write and the net10+ span-escape write.</returns>
    /// <remarks>net6+: an unformatted integer renders as URL-safe digits, so it is written with no escaping.
    /// net10+: any <c>ISpanFormattable</c> renders into a stack buffer and escapes span-to-string, skipping the ToString.</remarks>
    private static (bool UrlSafe, bool Escapable) ComputeSpanFormattableTiers(
        ITypeSymbol type,
        string? format,
        bool isNullableValueType,
        bool implementsSpanFormattable,
        InterfaceGenerationContext context)
    {
        var urlSafe = context.SpanFormattableSymbol is not null
            && !isNullableValueType
            && format is null
            && type.SpecialType is >= SpecialType.System_SByte and <= SpecialType.System_UInt64;
        var escapable = context.SupportsSpanEscape
            && !isNullableValueType
            && implementsSpanFormattable;
        return (urlSafe, escapable);
    }

    /// <summary>Resolves the compile-time enum members honored by the default URL parameter formatter.</summary>
    /// <param name="enumType">The enum type symbol.</param>
    /// <returns>The member models, or null when duplicate constants make a compile-time switch unfaithful.</returns>
    private static ImmutableEquatableArray<EnumFormatMemberModel>? BuildEnumFormatMembers(ITypeSymbol enumType)
    {
        var members = new List<EnumFormatMemberModel>();
        var seenValues = new HashSet<object>();

        foreach (var member in enumType.GetMembers())
        {
            if (member is not IFieldSymbol { HasConstantValue: true } field)
            {
                continue;
            }

            // Enum.GetName picks an unspecified alias for duplicated constants, so a compile-time switch
            // cannot reproduce the runtime formatter faithfully; fall back to the formatter for such enums.
            if (!seenValues.Add(field.ConstantValue!))
            {
                return null;
            }

            members.Add(new(field.Name, GetEnumMemberOverride(field)));
        }

        return members.ToImmutableEquatableArray();
    }

    /// <summary>Reads the <c>[EnumMember(Value = ...)]</c> override for an enum field.</summary>
    /// <param name="field">The enum field symbol.</param>
    /// <returns>The override value, or null when absent (or declared without a value).</returns>
    private static string? GetEnumMemberOverride(IFieldSymbol field)
    {
        foreach (var attribute in field.GetAttributes())
        {
            if (attribute.AttributeClass!.ToDisplayString() != EnumMemberAttributeDisplayName)
            {
                continue;
            }

            foreach (var named in attribute.NamedArguments)
            {
                if (named.Key == "Value" && named.Value.Value is string value)
                {
                    return value;
                }
            }

            return null;
        }

        return null;
    }

    /// <summary>Tries to resolve the single <c>IEnumerable&lt;T&gt;</c> element type of a parameter type.</summary>
    /// <param name="type">The parameter type.</param>
    /// <param name="elementType">Receives the element type.</param>
    /// <returns><see langword="true"/> when exactly one generic enumerable element type is implemented.</returns>
    private static bool TryGetEnumerableElementType(
        ITypeSymbol type,
        out ITypeSymbol? elementType)
    {
        if (type is IArrayTypeSymbol { Rank: 1 } array)
        {
            elementType = array.ElementType;
            return true;
        }

        elementType = null;
        if (type is INamedTypeSymbol named && IsGenericEnumerable(named))
        {
            elementType = named.TypeArguments[0];
        }

        foreach (var implemented in type.AllInterfaces)
        {
            if (!IsGenericEnumerable(implemented))
            {
                continue;
            }

            var candidate = implemented.TypeArguments[0];
            if (elementType is null)
            {
                elementType = candidate;
                continue;
            }

            // A type implementing several distinct IEnumerable<T> closes over an ambiguous element type, and
            // the reflection path's interface-order behavior is unspecified, so fall back.
            if (!SymbolEqualityComparer.Default.Equals(elementType, candidate))
            {
                elementType = null;
                return false;
            }
        }

        return elementType is not null;
    }

    /// <summary>Determines whether a type is a closed <c>System.Collections.Generic.IEnumerable&lt;T&gt;</c>.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is the generic enumerable interface.</returns>
    private static bool IsGenericEnumerable(ITypeSymbol type) =>
        type is INamedTypeSymbol
        {
            TypeKind: TypeKind.Interface,
            Name: "IEnumerable",
            Arity: 1,
            ContainingNamespace.Name: "Generic",
            ContainingNamespace.ContainingNamespace.Name: "Collections",
            ContainingNamespace.ContainingNamespace.ContainingNamespace.Name: "System"
        };

    /// <summary>Determines whether collection elements need a null check before formatting.</summary>
    /// <param name="elementType">The element type.</param>
    /// <returns><see langword="true"/> for reference and nullable-value elements.</returns>
    private static bool CanElementBeNull(ITypeSymbol elementType) =>
        !elementType.IsValueType
        || elementType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };

    /// <summary>Parses the query-relevant data from a parameter's <c>[Query]</c> attribute, if present.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <returns>The parsed data, or defaults when the attribute is absent.</returns>
    private static QueryFormData ParseParameterQueryData(IParameterSymbol parameter)
    {
        var attribute = FindParameterAttribute(parameter, QueryAttributeDisplayName);
        return attribute is null ? default : ParseFormQueryAttribute(attribute);
    }
}
