// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>Dotted <c>{param.Prop}</c> object path placeholder binding.</content>
internal static partial class Parser
{
    /// <summary>Classifies a dotted <c>{param.Prop}</c> object parameter into its path bindings and residual query.</summary>
    /// <param name="parameter">The enclosing object parameter.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="context">The lookup state carrying the placeholder locations and generation context.</param>
    /// <returns>
    /// The parsed path-object parameter carrying its property-to-path bindings and, for any property not bound to the
    /// path, a flattened residual query. Falls back when a placeholder property is unresolvable or non-simple, or when
    /// a residual property has a shape the query flattener cannot render inline.
    /// </returns>
    private static ParsedRequestParameter BuildPathObjectBinding(
        IParameterSymbol parameter,
        string parameterType,
        in LooseParameterContext context)
    {
        if (TryBuildPathObjectBindings(parameter, context.UrlName, context) is not { } bindings)
        {
            return new(UnsupportedRequestParameter(parameter, parameterType, context.Generation), false, 0, 0, 0);
        }

        var boundPropertyNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var binding in bindings)
        {
            _ = boundPropertyNames.Add(binding.TopLevelClrName);
        }

        // The reflection builder splits a bound object between the path and the query: matched properties fill the
        // placeholders, and every other public readable property is flattened into the query string. Mirror that by
        // flattening the declared type's residual properties; an unsupported residual shape falls the whole parameter
        // back so the query is never emitted partially.
        if (!TryBuildPathResidualQuery(parameter, context.UrlName, boundPropertyNames, context.FormattableSymbol, context.Generation, out var residualQuery))
        {
            return new(UnsupportedRequestParameter(parameter, parameterType, context.Generation), false, 0, 0, 0);
        }

        var model = BuildPathObjectParameter(parameter, parameterType, bindings, context.Generation);
        return new(residualQuery is null ? model : model with { Query = residualQuery }, true, 0, 0, 0);
    }

    /// <summary>Flattens the properties of a path-bound object that are not consumed by a path placeholder into a query.</summary>
    /// <param name="parameter">The enclosing object parameter.</param>
    /// <param name="urlName">The parameter's resolved URL name.</param>
    /// <param name="boundPropertyNames">The property names already bound to path placeholders, excluded from the query.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <param name="residualQuery">Receives the residual object query, or null when every property is bound to the path.</param>
    /// <returns><see langword="false"/> when a residual property cannot flatten inline and the parameter must fall back.</returns>
    private static bool TryBuildPathResidualQuery(
        IParameterSymbol parameter,
        string urlName,
        HashSet<string> boundPropertyNames,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context,
        out QueryParameterModel? residualQuery)
    {
        residualQuery = null;

        var data = ParseParameterQueryData(parameter);
        var parameterPrefixSegment = string.IsNullOrWhiteSpace(data.Prefix) ? null : data.Prefix + data.Delimiter;

        // A path-bound property is always a simple scalar, so it flattens here without issue; a null result therefore
        // means a residual property has an unsupported shape, which falls the whole parameter back to reflection.
        var properties = TryBuildQueryObjectProperties(parameter.Type, parameterPrefixSegment, formattableSymbol, context);
        if (properties is null)
        {
            return false;
        }

        var residual = new List<QueryObjectPropertyModel>();
        foreach (var property in properties)
        {
            if (!boundPropertyNames.Contains(property.ClrName))
            {
                residual.Add(property);
            }
        }

        if (residual.Count == 0)
        {
            return true;
        }

        residualQuery = new(
            urlName,
            QueryParameterShape.Object,
            TreatAsString: false,
            HasParameterAttribute(parameter, EncodedAttributeDisplayName),
            data.CollectionFormatValue,
            ElementCanBeNull: false,
            BuildValueFormat(parameter.Type, null, formattableSymbol, context),
            residual.ToImmutableEquatableArray(),
            NestingDelimiter: string.IsNullOrEmpty(data.Delimiter) ? "." : data.Delimiter);
        return true;
    }

    /// <summary>Resolves the dotted <c>{param.Prop}</c> placeholders bound to a parameter into path property bindings.</summary>
    /// <param name="parameter">The enclosing object parameter.</param>
    /// <param name="urlName">The parameter's resolved URL name.</param>
    /// <param name="context">The lookup state carrying the placeholder locations and generation context.</param>
    /// <returns>The property bindings, or null when any placeholder cannot be resolved to a simple readable property.</returns>
    private static ImmutableEquatableArray<PathObjectBindingModel>? TryBuildPathObjectBindings(
        IParameterSymbol parameter,
        string urlName,
        in LooseParameterContext context)
    {
        var prefix = urlName + ".";
        var bindings = new List<PathObjectBindingModel>();
        foreach (var placeholder in context.ParameterLocations)
        {
            var key = placeholder.Key;
            if (key.Length <= prefix.Length || !key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // A single-level {param.Prop} binds a direct property; a dotted {param.a.b} binds a nested chain, walked
            // with null-conditional access so a null link renders empty exactly as the reflection builder does.
            var propertyPath = key[prefix.Length..];
            if (TryResolvePathPropertyChain(parameter.Type, propertyPath, context) is not { } chain)
            {
                return null;
            }

            foreach (var location in placeholder.Value)
            {
                bindings.Add(new(location, chain.AccessPath, chain.TopLevelName, chain.PropertyType, chain.ValueFormat, chain.CanBeNull));
            }
        }

        if (bindings.Count == 0)
        {
            return null;
        }

        // The path builder appends fragments in template order, so bindings must be sorted by placeholder position.
        bindings.Sort(static (left, right) => left.Location.Start.Value.CompareTo(right.Location.Start.Value));
        return bindings.ToImmutableEquatableArray();
    }

    /// <summary>Resolves a dotted path placeholder (single or nested) into its access expression and formatting metadata.</summary>
    /// <param name="rootType">The enclosing object parameter's type.</param>
    /// <param name="propertyPath">The placeholder text after the parameter name (e.g. <c>Slug</c> or <c>a.b</c>).</param>
    /// <param name="context">The lookup state carrying the generation context.</param>
    /// <returns>The resolved access path, top-level property, final type, formatting strategy and nullability, or null.</returns>
    private static (string AccessPath, string TopLevelName, string PropertyType, InlineValueFormatModel ValueFormat, bool CanBeNull)?
        TryResolvePathPropertyChain(ITypeSymbol rootType, string propertyPath, in LooseParameterContext context)
    {
        var segments = propertyPath.Split('.');
        if (FindReadablePathProperty(rootType, segments[0]) is not { } firstProperty)
        {
            return null;
        }

        var names = new List<string>(segments.Length) { firstProperty.Name };
        var finalProperty = firstProperty;
        var currentType = finalProperty.Type;
        for (var i = 1; i < segments.Length; i++)
        {
            if (FindReadablePathProperty(currentType, segments[i]) is not { } property)
            {
                return null;
            }

            names.Add(property.Name);
            finalProperty = property;
            currentType = property.Type;
        }

        if (!IsSimpleType(finalProperty.Type, context.FormattableSymbol))
        {
            return null;
        }

        var propertyType = QualifyType(finalProperty.Type, context.Generation);
        var canBeNull = CanBeNull(finalProperty.Type, finalProperty.NullableAnnotation);

        // A single-level binding keeps the span-formattable fast path. A nested chain is formatted through the URL
        // parameter formatter (FormatterOnly): '?.' between links already yields null for a missing intermediate, so the
        // fast '.Value' unwrap - which would bind to the wrong link - is skipped, matching the reflection builder's walk.
        if (segments.Length == 1)
        {
            return (names[0], names[0], propertyType, BuildValueFormat(finalProperty.Type, null, context.FormattableSymbol, context.Generation), canBeNull);
        }

        var formatterOnly = new InlineValueFormatModel(InlineFormatKind.FormatterOnly, null, propertyType, IsNullableValueType: false, EnumMembers: null);
        return (string.Join("?.", names), names[0], propertyType, formatterOnly, canBeNull);
    }

    /// <summary>Finds a public readable property on a type or its base types by case-insensitive name.</summary>
    /// <param name="type">The declared parameter type.</param>
    /// <param name="propertyName">The property name from the dotted placeholder.</param>
    /// <returns>The matching property, or null when none is readable.</returns>
    private static IPropertySymbol? FindReadablePathProperty(ITypeSymbol type, string propertyName)
    {
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IPropertySymbol property
                    && string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase)
                    && IsReadableFormProperty(property))
                {
                    return property;
                }
            }
        }

        return null;
    }

    /// <summary>Builds the path parameter model for an object whose dotted placeholders bind its properties.</summary>
    /// <param name="parameter">The enclosing object parameter.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="bindings">The resolved property bindings.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The path parameter model carrying the property bindings.</returns>
    private static RequestParameterModel BuildPathObjectParameter(
        IParameterSymbol parameter,
        string parameterType,
        ImmutableEquatableArray<PathObjectBindingModel> bindings,
        InterfaceGenerationContext context) =>
        new(
            parameter.MetadataName,
            parameterType,
            null,
            BuildParameterAttributes(parameter, context),
            RequestParameterKind.Path,
            CanBeNull(parameter.Type, parameter.NullableAnnotation),
            string.Empty,
            string.Empty,
            string.Empty,
            BodyBufferMode.None)
        {
            PathObjectBindings = bindings,
        };
}
