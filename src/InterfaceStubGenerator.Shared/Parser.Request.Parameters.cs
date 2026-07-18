// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Refit.Generator;

/// <summary>Parses candidate interfaces and methods into the models used to generate Refit stubs.</summary>
/// <content>Classifies each method parameter into its request binding for the generated inline path.</content>
internal static partial class Parser
{
    /// <summary>Parses request parameter bindings for the generated inline path.</summary>
    /// <param name="parameters">The method parameters.</param>
    /// <param name="parameterLocations">The placeholder names in the URL with their locations.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol used to classify inline-eligible path parameter types, or null when unavailable.</param>
    /// <param name="allowImplicitBody">Whether an un-attributed complex parameter becomes the implicit request body.</param>
    /// <param name="isMultipart">Whether the method is multipart, so un-attributed parameters become form parts.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <param name="canGenerateInline">Receives whether every parameter is supported.</param>
    /// <returns>The parsed request parameter models.</returns>
    internal static ImmutableEquatableArray<RequestParameterModel> ParseRequestParameters(
        in ImmutableArray<IParameterSymbol> parameters,
        Dictionary<string, List<Range>> parameterLocations,
        INamedTypeSymbol? formattableSymbol,
        bool allowImplicitBody,
        bool isMultipart,
        InterfaceGenerationContext context,
        out bool canGenerateInline)
    {
        if (parameters.IsEmpty)
        {
            canGenerateInline = true;
            return ImmutableEquatableArray<RequestParameterModel>.Empty;
        }

        // An explicit [Body] anywhere suppresses implicit body detection, matching the reflection builder.
        var implicitBodyEligible = allowImplicitBody && !HasExplicitBodyParameter(parameters);

        var requestParameters = new RequestParameterModel[parameters.Length];
        var bodyCount = 0;
        var cancellationTokenCount = 0;
        var headerCollectionCount = 0;
        var implicitBodyAssigned = false;
        canGenerateInline = true;

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var name = ResolveUrlName(parameter);
            _ = parameterLocations.TryGetValue(name, out var location);
            List<Range>? roundTripLocation = null;
            if (location is null)
            {
                _ = parameterLocations.TryGetValue("**" + name, out roundTripLocation);
            }

            var classification = new LooseParameterContext(
                name,
                location?.ToImmutableEquatableArray(),
                roundTripLocation?.ToImmutableEquatableArray(),
                parameterLocations,
                formattableSymbol,
                implicitBodyEligible,
                isMultipart,
                context);
            var parsedParameter = ParseRequestParameter(parameter, classification, ref implicitBodyAssigned);
            requestParameters[i] = parsedParameter.Parameter;
            bodyCount += parsedParameter.BodyCount;
            cancellationTokenCount += parsedParameter.CancellationTokenCount;
            headerCollectionCount += parsedParameter.HeaderCollectionCount;
            canGenerateInline &= parsedParameter.CanGenerateInline;
        }

        // More than one body, cancellation token, header collection, or [Authorize] parameter is an invalid
        // definition the reflection builder rejects; fall back so its validation still throws.
        canGenerateInline &= HasInlineableParameterCounts(
            bodyCount,
            cancellationTokenCount,
            headerCollectionCount,
            requestParameters);

        return ImmutableEquatableArrayFactory.FromArray(requestParameters);
    }

    /// <summary>Determines whether the single-instance parameter counts allow inline generation.</summary>
    /// <param name="bodyCount">The number of body parameters.</param>
    /// <param name="cancellationTokenCount">The number of cancellation token parameters.</param>
    /// <param name="headerCollectionCount">The number of header collection parameters.</param>
    /// <param name="parameters">The parsed request parameters, scanned for <c>[Authorize]</c> parameters.</param>
    /// <returns><see langword="true"/> when no single-instance binding appears more than once.</returns>
    internal static bool HasInlineableParameterCounts(
        int bodyCount,
        int cancellationTokenCount,
        int headerCollectionCount,
        RequestParameterModel[] parameters) =>
        bodyCount <= 1
        && cancellationTokenCount <= 1
        && headerCollectionCount <= 1
        && CountAuthorizeParameters(parameters) <= 1;

    /// <summary>Counts the <c>[Authorize]</c> parameters (Authorization headers carrying a scheme prefix).</summary>
    /// <param name="parameters">The parsed request parameters.</param>
    /// <returns>The number of <c>[Authorize]</c> parameters.</returns>
    internal static int CountAuthorizeParameters(RequestParameterModel[] parameters)
    {
        var count = 0;
        foreach (var parameter in parameters)
        {
            if (parameter is { Kind: RequestParameterKind.Header, HeaderValuePrefix: not null })
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Resolves a parameter's URL name, honoring an <c>[AliasAs]</c> attribute.</summary>
    /// <param name="parameter">The parameter symbol.</param>
    /// <returns>The alias name or the declared parameter name.</returns>
    internal static string ResolveUrlName(IParameterSymbol parameter)
    {
        var aliasAttr = FindParameterAttribute(parameter, AliasAsAttributeDisplayName);
        return aliasAttr is not null ? GetFirstStringArgument(aliasAttr) ?? parameter.Name : parameter.Name;
    }

    /// <summary>Determines whether any parameter carries an explicit <c>[Body]</c> attribute.</summary>
    /// <param name="parameters">The method parameters.</param>
    /// <returns><see langword="true"/> when an explicit body parameter exists.</returns>
    internal static bool HasExplicitBodyParameter(in ImmutableArray<IParameterSymbol> parameters)
    {
        foreach (var parameter in parameters)
        {
            if (HasParameterAttribute(parameter, BodyAttributeDisplayName))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Parses one request parameter binding.</summary>
    /// <param name="parameter">The parameter to parse.</param>
    /// <param name="context">The lookup state used to classify the parameter.</param>
    /// <param name="implicitBodyAssigned">Tracks whether an earlier parameter already claimed the implicit body.</param>
    /// <returns>The parsed parameter and eligibility counters.</returns>
    internal static ParsedRequestParameter ParseRequestParameter(
        IParameterSymbol parameter,
        in LooseParameterContext context,
        ref bool implicitBodyAssigned)
    {
        var parameterType = QualifyType(parameter.Type, context.Generation);
        if (IsCancellationToken(parameter.Type))
        {
            return CancellationTokenParameter(parameter, parameterType, context.Locations, context.Generation);
        }

        // A [Url] parameter supplies the absolute request URI. Only a string or Uri can be emitted inline; any other
        // type falls back to the reflection builder (eligibility false), whose validation throws for an invalid value.
        if (TryParseUrlParameter(parameter, parameterType, context.Generation, out var urlParameter))
        {
            return new(urlParameter, IsInlineUrlType(parameter.Type), 0, 0, 0);
        }

        if (TryParseBodyParameter(parameter, parameterType, context.Generation, out var bodyParameter))
        {
            // A form-url-encoded body of a type that references a type parameter would emit
            // CreateUrlEncodedBodyContent<T>, whose [DynamicallyAccessedMembers(PublicProperties)] an open type
            // parameter cannot satisfy (IL2091), so it keeps using the reflection request builder.
            var bodyEligible = bodyParameter.BodySerializationMethod != "UrlEncoded"
                || !ReferencesTypeParameter(parameter.Type);
            return new(bodyParameter, bodyEligible, 1, 0, 0);
        }

        if (TryParseHeaderParameter(parameter, parameterType, context.Generation, out var headerParameter))
        {
            return new(headerParameter, true, 0, 0, 0);
        }

        if (TryParseHeaderCollectionParameter(parameter, parameterType, context.Generation, out var headerCollectionParameter))
        {
            return new(
                headerCollectionParameter,
                headerCollectionParameter.Kind == RequestParameterKind.HeaderCollection,
                0,
                0,
                1);
        }

        return TryParsePropertyParameter(parameter, parameterType, context.Generation, out var propertyParameter)
            ? ParsePropertyQueryBinding(parameter, parameterType, context.UrlName, context.FormattableSymbol, context.Generation, propertyParameter)
            : ParseBoundPathParameter(parameter, parameterType, context)
                ?? ClassifyLooseParameter(parameter, parameterType, context, ref implicitBodyAssigned);
    }

    /// <summary>Builds the cancellation token parameter binding.</summary>
    /// <param name="parameter">The parameter symbol.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="locations">The parameter's placeholder locations, if any.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The parsed parameter and eligibility counters.</returns>
    internal static ParsedRequestParameter CancellationTokenParameter(
        IParameterSymbol parameter,
        string parameterType,
        ImmutableEquatableArray<Range>? locations,
        InterfaceGenerationContext context) =>
        new(
            new(
                parameter.MetadataName,
                parameterType,
                locations,
                BuildParameterAttributes(parameter, context),
                RequestParameterKind.CancellationToken,
                CanBeNull(parameter.Type, parameter.NullableAnnotation),
                string.Empty,
                string.Empty,
                string.Empty,
                BodyBufferMode.None),
            true,
            0,
            1,
            0);

    /// <summary>Parses a parameter bound to a path placeholder, when one exists.</summary>
    /// <param name="parameter">The parameter to parse.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="context">The lookup state used to classify the parameter.</param>
    /// <returns>The parsed path binding, or <see langword="null"/> when the parameter has no placeholder.</returns>
    internal static ParsedRequestParameter? ParseBoundPathParameter(
        IParameterSymbol parameter,
        string parameterType,
        in LooseParameterContext context) =>
        context switch
        {
            { Locations: { } locations } => ParseDirectPathParameter(parameter, parameterType, locations, context),
            { RoundTripLocations: { } roundTripLocations } => ParseRoundTripPathParameter(parameter, parameterType, roundTripLocations, context),
            _ => null,
        };

    /// <summary>Parses a parameter bound to a plain <c>{name}</c> placeholder.</summary>
    /// <param name="parameter">The parameter to parse.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="locations">The placeholder locations.</param>
    /// <param name="context">The lookup state used to classify the parameter.</param>
    /// <returns>The parsed parameter and eligibility counters.</returns>
    internal static ParsedRequestParameter ParseDirectPathParameter(
        IParameterSymbol parameter,
        string parameterType,
        ImmutableEquatableArray<Range> locations,
        in LooseParameterContext context) =>
        CanInlinePathParameterType(parameter.Type, context.FormattableSymbol)
            ? new(
                PathRequestParameter(parameter, parameterType, locations, context.Generation) with
                {
                    ValueFormat = BuildValueFormat(parameter.Type, NormalizeFormat(ParseParameterQueryData(parameter).Format), context.FormattableSymbol, context.Generation),
                    PreEncoded = HasParameterAttribute(parameter, EncodedAttributeDisplayName),
                },
                true,
                0,
                0,
                0)
            : new(UnsupportedRequestParameter(parameter, parameterType, context.Generation), false, 0, 0, 0);

    /// <summary>Determines whether a plain <c>{name}</c> path parameter can be bound inline.</summary>
    /// <param name="type">The declared parameter type.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <returns><see langword="true"/> when the value stringifies with the same result as the reflection builder.</returns>
    /// <remarks>
    /// A simple type formats through the fast path. Any concrete class, struct, or array (including a collection like
    /// <c>List&lt;int&gt;</c> or <c>byte[]</c>) also binds inline: the reflection builder renders a route value with
    /// <c>UrlParameterFormatter.Format(value, ...)</c>, which for a non-<c>IFormattable</c> value is <c>value.ToString()</c>
    /// - a virtual call that dispatches to the runtime type in the generated code exactly as it does in the reflection
    /// builder, so a runtime subtype (and a collection's <c>System.Collections…</c> text) renders identically. (The only
    /// divergence is the vanishing case of a subtype whose <c>IFormattable.ToString(null, invariant)</c> differs from its
    /// parameterless <c>ToString()</c>.) An <c>object</c>, interface, or open generic type stays on the reflection path -
    /// it has no usable declared shape.
    /// </remarks>
    internal static bool CanInlinePathParameterType(ITypeSymbol type, INamedTypeSymbol? formattableSymbol) =>
        IsSimpleType(type, formattableSymbol)
        || (type.SpecialType != SpecialType.System_Object
            && type.TypeKind is TypeKind.Class or TypeKind.Struct or TypeKind.Array);

    /// <summary>Parses a parameter bound to a round-tripping <c>{**name}</c> placeholder.</summary>
    /// <remarks>Round-tripping normally needs the reflection builder's per-segment escaping, but an
    /// <c>[Encoded]</c> string value passes through verbatim, so it becomes a plain inline substitution.</remarks>
    /// <param name="parameter">The parameter to parse.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="roundTripLocations">The placeholder locations.</param>
    /// <param name="context">The lookup state used to classify the parameter.</param>
    /// <returns>The parsed parameter and eligibility counters.</returns>
    internal static ParsedRequestParameter ParseRoundTripPathParameter(
        IParameterSymbol parameter,
        string parameterType,
        ImmutableEquatableArray<Range> roundTripLocations,
        in LooseParameterContext context)
    {
        var encoded = HasParameterAttribute(parameter, EncodedAttributeDisplayName);

        // [Encoded] keeps the caller-encoded string verbatim (string only). A non-[Encoded] catch-all splits the
        // value's string form on '/', formatting and escaping each segment while preserving the separators, exactly
        // as the reflection builder does — so any type is supported inline.
        return encoded && parameter.Type.SpecialType != SpecialType.System_String
            ? new(UnsupportedRequestParameter(parameter, parameterType, context.Generation), false, 0, 0, 0)
            : new(
                PathRequestParameter(parameter, parameterType, roundTripLocations, context.Generation) with
                {
                    ValueFormat = BuildValueFormat(parameter.Type, null, context.FormattableSymbol, context.Generation),
                    PreEncoded = true,
                    IsRoundTrip = !encoded,
                },
                true,
                0,
                0,
                0);
    }

    /// <summary>Classifies a parameter with no path binding as a flag, implicit body, query value, or fallback.</summary>
    /// <param name="parameter">The parameter to classify.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="context">The lookup state used to classify the parameter.</param>
    /// <param name="implicitBodyAssigned">Tracks whether an earlier parameter already claimed the implicit body.</param>
    /// <returns>The parsed parameter and eligibility counters.</returns>
    internal static ParsedRequestParameter ClassifyLooseParameter(
        IParameterSymbol parameter,
        string parameterType,
        in LooseParameterContext context,
        ref bool implicitBodyAssigned)
    {
        // Dotted {param.Prop} placeholders bind declared properties into the path; each property is formatted and
        // escaped just like a scalar path parameter, and any property left unbound flattens into the query string.
        // An unresolvable or non-simple placeholder property, or an unsupported residual property, falls back.
        if (HasDottedPlaceholderFor(context.ParameterLocations, context.UrlName))
        {
            return BuildPathObjectBinding(parameter, parameterType, context);
        }

        // [Authorize] emits an Authorization header of "{scheme} {value}"; the scheme is a compile-time constant.
        if (FindParameterAttribute(parameter, AuthorizeAttributeDisplayName) is { } authorizeAttribute)
        {
            var scheme = GetFirstStringArgument(authorizeAttribute) ?? DefaultAuthorizeScheme;
            return new(
                BuildAuthorizeHeaderParameter(parameter, parameterType, scheme, context.Generation),
                true,
                0,
                0,
                0);
        }

        // In a multipart method every remaining parameter is a form part unless it carries [Query]; this replaces
        // the implicit-body and auto-query classification the non-multipart path applies below.
        if (context.IsMultipart)
        {
            return ClassifyMultipartParameter(parameter, parameterType, context);
        }

        if (HasParameterAttribute(parameter, QueryNameAttributeDisplayName))
        {
            var flagModel = BuildFlagModel(parameter, context.FormattableSymbol, context.Generation);
            return new(QueryRequestParameter(parameter, parameterType, flagModel, context.Generation), true, 0, 0, 0);
        }

        // Body resolution precedes query mapping in the reflection builder: on POST/PUT/PATCH the first
        // un-attributed complex parameter is the implicit body; a second one throws there, so fall back.
        if (context.ImplicitBodyEligible && IsImplicitBodyCandidate(parameter))
        {
            return ClaimImplicitBody(parameter, parameterType, context.Generation, ref implicitBodyAssigned);
        }

        return TryBuildQueryModel(parameter, context.UrlName, context.FormattableSymbol, context.Generation, out var query)
            ? new(QueryRequestParameter(parameter, parameterType, query!, context.Generation), true, 0, 0, 0)
            : new(UnsupportedRequestParameter(parameter, parameterType, context.Generation), false, 0, 0, 0);
    }

    /// <summary>Determines whether a parameter matches the reflection builder's implicit body candidacy rules.</summary>
    /// <param name="parameter">The parameter to inspect.</param>
    /// <returns><see langword="true"/> for un-attributed non-string reference-type parameters.</returns>
    internal static bool IsImplicitBodyCandidate(IParameterSymbol parameter) =>
        !parameter.Type.IsValueType
        && parameter.Type.SpecialType != SpecialType.System_String
        && !HasParameterAttribute(parameter, QueryAttributeDisplayName)
        && !HasParameterAttribute(parameter, QueryConverterAttributeDisplayName);

    /// <summary>Claims the implicit body slot, falling back when an earlier parameter already claimed it.</summary>
    /// <param name="parameter">The parameter to bind.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <param name="implicitBodyAssigned">Tracks whether an earlier parameter already claimed the implicit body.</param>
    /// <returns>The parsed parameter and eligibility counters.</returns>
    internal static ParsedRequestParameter ClaimImplicitBody(
        IParameterSymbol parameter,
        string parameterType,
        InterfaceGenerationContext context,
        ref bool implicitBodyAssigned)
    {
        if (implicitBodyAssigned)
        {
            return new(UnsupportedRequestParameter(parameter, parameterType, context), false, 0, 0, 0);
        }

        implicitBodyAssigned = true;
        return new(ImplicitBodyRequestParameter(parameter, parameterType, context), true, 1, 0, 0);
    }

    /// <summary>Attaches query-binding metadata to a property parameter that also carries <c>[Query]</c>.</summary>
    /// <param name="parameter">The parameter symbol.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="urlName">The resolved URL name.</param>
    /// <param name="formattableSymbol">The resolved <c>System.IFormattable</c> symbol, or null when unavailable.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <param name="propertyParameter">The parsed property parameter model.</param>
    /// <returns>The parsed parameter and eligibility counters.</returns>
    internal static ParsedRequestParameter ParsePropertyQueryBinding(
        IParameterSymbol parameter,
        string parameterType,
        string urlName,
        INamedTypeSymbol? formattableSymbol,
        InterfaceGenerationContext context,
        RequestParameterModel propertyParameter)
    {
        if (!HasParameterAttribute(parameter, QueryAttributeDisplayName))
        {
            return new(propertyParameter, true, 0, 0, 0);
        }

        // A [Property] parameter that also carries [Query] feeds both the request options and the query string.
        return TryBuildQueryModel(parameter, urlName, formattableSymbol, context, out var propertyQuery)
            ? new(propertyParameter with { Query = propertyQuery }, true, 0, 0, 0)
            : new(UnsupportedRequestParameter(parameter, parameterType, context), false, 0, 0, 0);
    }

    /// <summary>Builds a query request parameter model.</summary>
    /// <param name="parameter">The parameter symbol.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="query">The query-binding metadata.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The query request parameter model.</returns>
    internal static RequestParameterModel QueryRequestParameter(
        IParameterSymbol parameter,
        string parameterType,
        QueryParameterModel query,
        InterfaceGenerationContext context) =>
        new(
            parameter.MetadataName,
            parameterType,
            null,
            BuildParameterAttributes(parameter, context),
            RequestParameterKind.Query,
            CanBeNull(parameter.Type, parameter.NullableAnnotation),
            string.Empty,
            string.Empty,
            string.Empty,
            BodyBufferMode.None)
        {
            Query = query,
        };

    /// <summary>Builds the implicit body parameter model for POST/PUT/PATCH methods.</summary>
    /// <param name="parameter">The parameter symbol.</param>
    /// <param name="parameterType">The parameter type display string.</param>
    /// <param name="context">The interface generation context, used to qualify extern-aliased types.</param>
    /// <returns>The implicit body parameter model.</returns>
    internal static RequestParameterModel ImplicitBodyRequestParameter(
        IParameterSymbol parameter,
        string parameterType,
        InterfaceGenerationContext context) =>
        new(
            parameter.MetadataName,
            parameterType,
            null,
            BuildParameterAttributes(parameter, context),
            RequestParameterKind.Body,
            CanBeNull(parameter.Type, parameter.NullableAnnotation),
            string.Empty,
            string.Empty,
            "Serialized",
            BodyBufferMode.Settings);
}
