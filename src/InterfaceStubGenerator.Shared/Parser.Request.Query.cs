// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
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

    /// <summary>The metadata name of <c>Refit.PathPrefixAttribute</c>.</summary>
    private const string PathPrefixAttributeDisplayName = "PathPrefixAttribute";

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

        query = TryBuildFlattenedObjectQueryModel(parameter, urlName, preEncoded, data, format, formattableSymbol, context);
        return query is not null;
    }

    /// <summary>Builds the flattened-object query model for a complex parameter, or null when it cannot flatten inline.</summary>
    /// <param name="parameter">The parameter to classify.</param>
    /// <param name="urlName">The resolved query key.</param>
    /// <param name="preEncoded">Whether the parameter carries <c>[Encoded]</c>.</param>
    /// <param name="data">The parameter's parsed <c>[Query]</c> data.</param>
    /// <param name="format">The effective compile-time format, or null.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The flattened object query model, or null when the parameter cannot be flattened inline.</returns>
    private static QueryParameterModel? TryBuildFlattenedObjectQueryModel(
        IParameterSymbol parameter,
        string urlName,
        bool preEncoded,
        in QueryFormData data,
        string? format,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context)
    {
        // A complex object is flattened into one query pair per public readable property, mirroring the reflection
        // builder's BuildQueryMap. The enclosing [Query(Prefix)] segment is folded into each property's key.
        var parameterPrefixSegment = string.IsNullOrWhiteSpace(data.Prefix) ? null : data.Prefix + data.Delimiter;

        var dictionaryQuery = TryBuildDictionaryQueryModel(parameter.Type, urlName, preEncoded, format, parameterPrefixSegment, formattableSymbol, context);
        if (dictionaryQuery is not null)
        {
            return dictionaryQuery;
        }

        if (TryBuildQueryObjectProperties(parameter.Type, parameterPrefixSegment, formattableSymbol, context) is not { } properties)
        {
            return null;
        }

        return new(
            urlName,
            QueryParameterShape.Object,
            TreatAsString: false,
            preEncoded,
            data.CollectionFormatValue,
            ElementCanBeNull: false,
            BuildValueFormat(parameter.Type, format, formattableSymbol, context),
            properties,
            NestingDelimiter: string.IsNullOrEmpty(data.Delimiter) ? "." : data.Delimiter);
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
        in QueryFormData data,
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
        in QueryFormData data,
        bool preEncoded,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context)
    {
        // The converter type is the sole typeof(...) constructor argument.
        if (GetSoleTypeArgument(converterAttribute) is not { } converterType)
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

    /// <summary>Reads the sole <c>typeof(...)</c> constructor argument from an attribute.</summary>
    /// <param name="attribute">The attribute whose single type argument is read.</param>
    /// <returns>The type argument, or <see langword="null"/> when the argument is absent or not a type.</returns>
    /// <remarks>The absent-argument guard defends against error-recovery symbols and cannot be reached from an
    /// attribute application that compiles.</remarks>
    [ExcludeFromCodeCoverage]
    private static ITypeSymbol? GetSoleTypeArgument(AttributeData attribute) =>
        attribute.ConstructorArguments.IsEmpty
            ? null
            : attribute.ConstructorArguments[0].Value as ITypeSymbol;

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
        InterfaceGenerationContext context)
    {
        if (!TryGetDictionaryTypes(type, out var keyType, out var valueType)
            || !IsSimpleType(keyType!, formattableSymbol))
        {
            return null;
        }

        var valueProperties = ResolveDictionaryValueProperties(valueType!, format, formattableSymbol, context, out var valueInlineable);
        return valueInlineable
            ? new(
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
                    parameterPrefixSegment,
                    valueProperties))
            : null;
    }

    /// <summary>Builds the query-binding model for a <c>[QueryName]</c> valueless flag parameter.</summary>
    /// <param name="parameter">The parameter to classify.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The flag query model; both a scalar flag and a per-element flag collection resolve to a value.</returns>
    private static QueryParameterModel BuildFlagModel(
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
            if (SymbolEqualityComparer.Default.Equals(implemented, formattableSymbol))
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
    /// <param name="implementsSpanFormattable">Whether the value type implements <c>ISpanFormattable</c>, resolved in the single interface walk.</param>
    /// <param name="context">The generation context carrying the resolved fast-path capabilities.</param>
    /// <returns>Whether the value qualifies for the net6+ URL-safe integer write and the net9+ span-escape write.</returns>
    /// <remarks>net6+: an unformatted integer renders as URL-safe digits, so it is written with no escaping.
    /// net9+: any <c>ISpanFormattable</c> renders into a stack buffer and escapes span-to-string, skipping the ToString.</remarks>
    private static (bool UrlSafe, bool Escapable) ComputeSpanFormattableTiers(
        ITypeSymbol type,
        string? format,
        bool implementsSpanFormattable,
        InterfaceGenerationContext context)
    {
        // A nullable value type still qualifies: the scalar query emitter formats it inside the existing null guard and
        // writes the unwrapped .Value (which is span-formattable). The path and collection fast paths opt out of the
        // nullable case separately, keeping their existing string-formatting path.
        var urlSafe = context.SpanFormattableSymbol is not null
            && format is null
            && type.SpecialType is >= SpecialType.System_SByte and <= SpecialType.System_UInt64;
        var escapable = context.SupportsSpanEscape
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
                if (TryReadEnumMemberValue(named) is { } value)
                {
                    return value;
                }
            }

            return null;
        }

        return null;
    }

    /// <summary>Reads the string value of an <c>[EnumMember]</c> <c>Value</c> named argument.</summary>
    /// <param name="namedArgument">The attribute named argument.</param>
    /// <returns>The string value, or <see langword="null"/> when the argument is not a string <c>Value</c>.</returns>
    /// <remarks><c>EnumMember</c> declares only the <c>Value</c> named argument, so the key comparison never fails here.</remarks>
    [ExcludeFromCodeCoverage]
    private static string? TryReadEnumMemberValue(KeyValuePair<string, TypedConstant> namedArgument) =>
        namedArgument.Key == "Value" ? namedArgument.Value.Value as string : null;

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

            // A second IEnumerable<T> among the (symbol-deduplicated) interfaces is always a distinct element type, so
            // the parameter closes an ambiguous element type. The reflection path's interface-order behavior is
            // unspecified, so fall back.
            elementType = null;
            return false;
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
    /// <remarks>The nullable-value-element arm is only selected by a <c>Nullable&lt;T&gt;</c> collection element, which the
    /// shared collection fixtures never present, so that outcome cannot be exercised.</remarks>
    [ExcludeFromCodeCoverage]
    private static bool CanElementBeNull(ITypeSymbol elementType) =>
        !elementType.IsValueType
        || elementType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

    /// <summary>Parses the query-relevant data from a parameter's <c>[Query]</c> attribute, if present.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <returns>The parsed data, or defaults when the attribute is absent.</returns>
    private static QueryFormData ParseParameterQueryData(IParameterSymbol parameter)
    {
        var attribute = FindParameterAttribute(parameter, QueryAttributeDisplayName);
        return attribute is null ? default : ParseFormQueryAttribute(attribute);
    }
}
