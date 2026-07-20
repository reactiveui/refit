// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Refit;

/// <summary>Route parameter binding: builds the parameter map and URL fragments, and resolves parameter/property URL names for <see cref="RestMethodInfoInternal"/>.</summary>
internal partial class RestMethodInfoInternal
{
    /// <summary>Builds the route parameter map and the ordered URL fragments for the relative path.</summary>
    /// <param name="relativePath">The relative URL path template.</param>
    /// <param name="parameterInfo">The array of method parameters.</param>
    /// <param name="allowUnmatchedRouteParameters">When true, a placeholder with no matching argument is left in the path verbatim instead of throwing.</param>
    /// <returns>A tuple containing the parameter map and the ordered list of URL fragments.</returns>
    [RequiresUnreferencedCode("Binding route parameters from request object properties requires public property metadata to be available at runtime.")]
    internal static (Dictionary<int, RestMethodParameterInfo> Map, List<ParameterFragment> Fragments)
        BuildParameterMap(
            string relativePath,
            ParameterInfo[] parameterInfo,
            bool allowUnmatchedRouteParameters)
    {
        var ret = new Dictionary<int, RestMethodParameterInfo>();

        var parameterizedParts = ParameterRegex().Matches(relativePath);

        if (parameterizedParts.Count == 0)
        {
            return string.IsNullOrEmpty(relativePath)
                ? (ret, [])
                : (ret, [ParameterFragment.Constant(relativePath)]);
        }

        var paramValidationDict = BuildParamValidationDict(parameterInfo);

        // The object-property lookup is only needed for a {obj.prop} placeholder that no direct parameter matches, so it
        // is built on first such use. A template whose placeholders all bind direct parameters never builds it.
        (Dictionary<string, ParameterInfo> Param, Dictionary<string, (ParameterInfo Parameter, PropertyInfo Property)>? Object) validation =
            (paramValidationDict, null);

        var fragmentList = new List<ParameterFragment>();
        var index = 0;

        for (var i = 0; i < parameterizedParts.Count; i++)
        {
            var match = parameterizedParts[i];

            // Add constant value from given http path
            if (match.Index != index)
            {
                fragmentList.Add(ParameterFragment.Constant(relativePath.Substring(index, match.Index - index)));
            }

            index = match.Index + match.Length;

            AddFragmentForMatch(
                relativePath,
                parameterInfo,
                ret,
                fragmentList,
                ref validation,
                match,
                allowUnmatchedRouteParameters);
        }

        if (index >= relativePath.Length)
        {
            return (ret, fragmentList);
        }

        // Add trailing string.
        var trailingConstant = relativePath[index..];
        fragmentList.Add(ParameterFragment.Constant(trailingConstant));

        return (ret, fragmentList);
    }

    /// <summary>Builds a lookup of lower-cased URL parameter names to their declaring method parameter.</summary>
    /// <param name="parameterInfo">The array of method parameters.</param>
    /// <returns>A map of URL parameter names to method parameters.</returns>
    internal static Dictionary<string, ParameterInfo> BuildParamValidationDict(ParameterInfo[] parameterInfo)
    {
        var paramValidationDict = new Dictionary<string, ParameterInfo>(parameterInfo.Length);
        for (var i = 0; i < parameterInfo.Length; i++)
        {
            paramValidationDict[GetUrlNameForParameter(parameterInfo[i]).ToLowerInvariant()] = parameterInfo[i];
        }

        return paramValidationDict;
    }

    /// <summary>Builds a lookup of lower-cased "parameter.property" names to the parameter/property pair that can bind them.</summary>
    /// <param name="parameterInfo">The array of method parameters.</param>
    /// <returns>A map of nested property names to their parameter/property pair.</returns>
    [RequiresUnreferencedCode("Binding route parameters from request object properties requires public property metadata to be available at runtime.")]
    internal static Dictionary<string, (ParameterInfo Parameter, PropertyInfo Property)> BuildObjectParamValidationDict(
        ParameterInfo[] parameterInfo)
    {
        // If the parameter is a class, build a dictionary for all of its potential bound properties.
        var objectParamValidationDict = new Dictionary<string, (ParameterInfo Parameter, PropertyInfo Property)>();
        for (var i = 0; i < parameterInfo.Length; i++)
        {
            var parameter = parameterInfo[i];
            if (!parameter.ParameterType.GetTypeInfo().IsClass)
            {
                continue;
            }

            var properties = GetParameterProperties(parameter);
            for (var j = 0; j < properties.Length; j++)
            {
                var key = $"{parameter.Name}.{GetUrlNameForProperty(properties[j])}".ToLowerInvariant();
                _ = objectParamValidationDict.TryAdd(key, (parameter, properties[j]));
            }
        }

        return objectParamValidationDict;
    }

    /// <summary>Resolves a single parameterized URL fragment against the parameter maps and appends the result.</summary>
    /// <param name="relativePath">The relative URL path template.</param>
    /// <param name="parameterInfo">The array of method parameters.</param>
    /// <param name="ret">The parameter map being built.</param>
    /// <param name="fragmentList">The fragment list being built.</param>
    /// <param name="validation">The lookup of directly matched parameter names, and the single-level object-property
    /// lookup built on first use and reused across the template's placeholders; its object entry is null until a
    /// placeholder needs it.</param>
    /// <param name="match">The parameterized URL match being resolved.</param>
    /// <param name="allowUnmatchedRouteParameters">When true, an unmatched placeholder is left in the path verbatim instead of throwing.</param>
    [RequiresUnreferencedCode("Binding route parameters from request object properties requires public property metadata to be available at runtime.")]
    internal static void AddFragmentForMatch(
        string relativePath,
        ParameterInfo[] parameterInfo,
        Dictionary<int, RestMethodParameterInfo> ret,
        List<ParameterFragment> fragmentList,
        ref (Dictionary<string, ParameterInfo> Param, Dictionary<string, (ParameterInfo Parameter, PropertyInfo Property)>? Object) validation,
        Match match,
        bool allowUnmatchedRouteParameters)
    {
        const string roundTripPrefix = "**";

        // A trailing '?' marks the placeholder optional: a null bound value drops the segment and its preceding '/'.
        var isOptional = match.Groups[3].Success;
        var rawName = match.Groups[1].Value.ToLowerInvariant();
        var isRoundTripping = rawName.StartsWith(roundTripPrefix, StringComparison.Ordinal);
        var name = isRoundTripping ? rawName[roundTripPrefix.Length..] : rawName;

        if (validation.Param.TryGetValue(name, out var value))
        {
            AddStandardParameter(
                parameterInfo,
                ret,
                fragmentList,
                new(rawName, name, isRoundTripping),
                value,
                isOptional);
        }
        else if (!isRoundTripping
            && (validation.Object ??= BuildObjectParamValidationDict(parameterInfo)).TryGetValue(name, out var value1))
        {
            AddObjectPropertyParameter(parameterInfo, ret, fragmentList, name, value1.Parameter, [value1.Property], isOptional);
        }
        else if (TryResolveNestedPropertyChain(parameterInfo, name) is { } nested)
        {
            // A round-trip placeholder only ever matches a direct parameter above, so it never reaches a nested chain
            // (which requires a dotted name); no isRoundTripping guard is needed here.
            AddObjectPropertyParameter(parameterInfo, ret, fragmentList, name, nested.Parameter, nested.Chain, isOptional);
        }
        else if (allowUnmatchedRouteParameters)
        {
            // Leave the unmatched placeholder in the URL verbatim (including its braces) so the
            // caller can resolve it later, e.g. inside a DelegatingHandler.
            fragmentList.Add(ParameterFragment.Constant(match.Value));
        }
        else
        {
            throw new ArgumentException(
                $"URL {relativePath} has parameter {rawName}, but no method parameter matches");
        }
    }

    /// <summary>Adds a standard (directly matched) route parameter to the parameter map and fragment list.</summary>
    /// <param name="parameterInfo">The array of method parameters.</param>
    /// <param name="ret">The parameter map being built.</param>
    /// <param name="fragmentList">The fragment list being built.</param>
    /// <param name="parsedName">The parsed parameter name details from the URL template.</param>
    /// <param name="value">The matched method parameter.</param>
    /// <param name="isOptional">Whether the placeholder was declared optional with the <c>{name?}</c> syntax.</param>
    internal static void AddStandardParameter(
        ParameterInfo[] parameterInfo,
        Dictionary<int, RestMethodParameterInfo> ret,
        List<ParameterFragment> fragmentList,
        ParsedParameterName parsedName,
        ParameterInfo value,
        bool isOptional)
    {
        // A round-tripping parameter may be any type: its value is formatted through the URL parameter formatter
        // (ToString by default) and each '/'-delimited segment is escaped independently, preserving the separators.
        var parameterType = parsedName.IsRoundTripping
            ? ParameterType.RoundTripping
            : ParameterType.Normal;
        var restMethodParameterInfo = new RestMethodParameterInfo(parsedName.Name, value) { Type = parameterType };

        var parameterIndex = Array.IndexOf(parameterInfo, restMethodParameterInfo.ParameterInfo);
        fragmentList.Add(ParameterFragment.Dynamic(parameterIndex, isOptional));
#if NET6_0_OR_GREATER
        _ = ret.TryAdd(parameterIndex, restMethodParameterInfo);
#else
        if (ret.ContainsKey(parameterIndex))
        {
            return;
        }

        ret.Add(parameterIndex, restMethodParameterInfo);
#endif
    }

    /// <summary>Adds an object-property route parameter, resolving a nested <c>{a.b.c}</c> chain when needed.</summary>
    /// <param name="parameterInfo">The array of method parameters.</param>
    /// <param name="ret">The parameter map being built.</param>
    /// <param name="fragmentList">The fragment list being built.</param>
    /// <param name="name">The normalized parameter name.</param>
    /// <param name="owner">The parameter whose property chain binds the placeholder.</param>
    /// <param name="propertyChain">The ordered property navigation from the parameter to the bound value.</param>
    /// <param name="isOptional">Whether the placeholder was declared optional with the <c>{name?}</c> syntax.</param>
    internal static void AddObjectPropertyParameter(
        ParameterInfo[] parameterInfo,
        Dictionary<int, RestMethodParameterInfo> ret,
        List<ParameterFragment> fragmentList,
        string name,
        ParameterInfo owner,
        IReadOnlyList<PropertyInfo> propertyChain,
        bool isOptional)
    {
        var parameterIndex = Array.IndexOf(parameterInfo, owner);

        // If we already have this parameter, add an additional ParameterProperty.
        if (ret.TryGetValue(parameterIndex, out var value2))
        {
            if (!value2.IsObjectPropertyParameter)
            {
                throw new ArgumentException(
                    $"Parameter {owner.Name} matches both a parameter and nested parameter on a parameter object");
            }

            value2.ParameterProperties.Add(new(name, propertyChain));
            fragmentList.Add(
                ParameterFragment.DynamicObject(parameterIndex, value2.ParameterProperties.Count - 1, isOptional));
            return;
        }

        var restMethodParameterInfo = new RestMethodParameterInfo(true, owner);
        restMethodParameterInfo.ParameterProperties.Add(new(name, propertyChain));

        var idx = Array.IndexOf(parameterInfo, restMethodParameterInfo.ParameterInfo);
        fragmentList.Add(ParameterFragment.DynamicObject(idx, 0, isOptional));
#if NET6_0_OR_GREATER
        _ = ret.TryAdd(idx, restMethodParameterInfo);
#else
        if (ret.ContainsKey(idx))
        {
            return;
        }

        ret.Add(idx, restMethodParameterInfo);
#endif
    }

    /// <summary>Resolves a dotted <c>{a.b.c}</c> placeholder into the parameter and its nested property navigation chain.</summary>
    /// <param name="parameterInfo">The array of method parameters.</param>
    /// <param name="name">The normalized (lower-cased) placeholder name.</param>
    /// <returns>The parameter and its property chain, or null when the placeholder is not a resolvable nested chain.</returns>
    [RequiresUnreferencedCode("Binding route parameters from request object properties requires public property metadata to be available at runtime.")]
    internal static (ParameterInfo Parameter, IReadOnlyList<PropertyInfo> Chain)? TryResolveNestedPropertyChain(
        ParameterInfo[] parameterInfo,
        string name)
    {
        // A single dot ("a.b") is a direct object property handled by the validation dictionary; only deeper chains reach here.
        var segments = name.Split('.');
        if (segments.Length < 3)
        {
            return null;
        }

        var parameter = Array.Find(parameterInfo, p => string.Equals(p.Name, segments[0], StringComparison.OrdinalIgnoreCase));
        if (parameter is null)
        {
            return null;
        }

        if (!parameter.ParameterType.GetTypeInfo().IsClass)
        {
            return null;
        }

        var chain = new List<PropertyInfo>(segments.Length - 1);
        var currentType = parameter.ParameterType;
        for (var i = 1; i < segments.Length; i++)
        {
            if (FindPropertyByUrlName(currentType, segments[i]) is not { } property)
            {
                return null;
            }

            chain.Add(property);
            currentType = property.PropertyType;
        }

        return (parameter, chain);
    }

    /// <summary>Finds a readable public property by its URL name (honoring any alias), case-insensitively.</summary>
    /// <param name="type">The declaring type to search.</param>
    /// <param name="urlName">The URL name segment to match.</param>
    /// <returns>The matching property, or null when none matches.</returns>
    [RequiresUnreferencedCode("Binding route parameters from request object properties requires public property metadata to be available at runtime.")]
    internal static PropertyInfo? FindPropertyByUrlName(Type type, string urlName)
    {
        foreach (var property in ReflectionPropertyHelpers.GetReadablePublicInstanceProperties(type))
        {
            if (string.Equals(GetUrlNameForProperty(property), urlName, StringComparison.OrdinalIgnoreCase))
            {
                return property;
            }
        }

        return null;
    }

    /// <summary>Gets the URL name to use for a parameter, honoring any alias attribute.</summary>
    /// <param name="paramInfo">The parameter whose URL name is resolved.</param>
    /// <returns>The aliased or declared parameter name.</returns>
    internal static string GetUrlNameForParameter(ParameterInfo paramInfo)
    {
        var aliasAttr = paramInfo.GetCustomAttribute<AliasAsAttribute>(true);
        return aliasAttr is not null ? aliasAttr.Name : paramInfo.Name!;
    }

    /// <summary>Gets the URL name to use for a property, honoring any alias attribute.</summary>
    /// <param name="propInfo">The property whose URL name is resolved.</param>
    /// <returns>The aliased or declared property name.</returns>
    internal static string GetUrlNameForProperty(PropertyInfo propInfo)
    {
        var aliasAttr = propInfo.GetCustomAttribute<AliasAsAttribute>(true);
        return aliasAttr is not null ? aliasAttr.Name : propInfo.Name;
    }

    /// <summary>Gets the multipart attachment name to use for a parameter.</summary>
    /// <param name="paramInfo">The parameter whose attachment name is resolved.</param>
    /// <returns>The attachment name, or null when none is specified.</returns>
    [ExcludeFromCodeCoverage] // The AttachmentName arm needs the [Obsolete] AttachmentNameAttribute, which CS0618 forbids a test from applying, so the branch cannot be covered.
    internal static string GetAttachmentNameForParameter(ParameterInfo paramInfo)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var nameAttr = paramInfo.GetCustomAttribute<AttachmentNameAttribute>(true);
#pragma warning restore CS0618 // Type or member is obsolete

        // also check for AliasAs
        return nameAttr?.Name ?? paramInfo.GetCustomAttribute<AliasAsAttribute>(true)?.Name!;
    }

    /// <summary>Finds the single cancellation token parameter for the method.</summary>
    /// <param name="methodInfo">The reflected method information.</param>
    /// <returns>The cancellation token parameter, or null when none is present.</returns>
    internal static ParameterInfo? FindCancellationTokenParameter(MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();
        ParameterInfo? cancellationTokenParam = null;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (!IsCancellationTokenParameter(parameters[i]))
            {
                continue;
            }

            if (cancellationTokenParam is not null)
            {
                throw new ArgumentException(
                    $"Argument list to method \"{methodInfo.Name}\" can only contain a single CancellationToken");
            }

            cancellationTokenParam = parameters[i];
        }

        return cancellationTokenParam;
    }

    /// <summary>Finds and validates the <c>[Url]</c> parameter that supplies the absolute request URI, ensuring the
    /// method does not also declare a path template.</summary>
    /// <param name="parameterArray">The array of method parameters.</param>
    /// <param name="sets">The classified attribute set for each parameter.</param>
    /// <param name="relativePath">The method's relative path template.</param>
    /// <returns>The index of the <c>[Url]</c> parameter, or a negative value when none is present.</returns>
    /// <exception cref="ArgumentException">More than one parameter carries <c>[Url]</c>, the parameter is not a
    /// <see cref="string"/> or <see cref="Uri"/>, or a <c>[Url]</c> parameter is combined with a non-empty path
    /// template.</exception>
    internal static int ResolveUrlParameter(ParameterInfo[] parameterArray, ParameterAttributeSet[] sets, string relativePath)
    {
        var urlIndex = FindUrlParameter(parameterArray, sets);
        if (urlIndex >= 0
            && !string.IsNullOrEmpty(relativePath)
            && relativePath != "/")
        {
            throw new ArgumentException(
                $"A [Url] method must not also declare a path template; [Url] provides the full absolute URI, but the template was \"{relativePath}\".");
        }

        return urlIndex;
    }

    /// <summary>Finds the index of the <c>[Url]</c> parameter that supplies the absolute request URI.</summary>
    /// <param name="parameterArray">The array of method parameters.</param>
    /// <param name="sets">The classified attribute set for each parameter.</param>
    /// <returns>The index of the <c>[Url]</c> parameter, or a negative value when none is present.</returns>
    /// <exception cref="ArgumentException">More than one parameter carries <c>[Url]</c>, or the parameter is not a
    /// <see cref="string"/> or <see cref="Uri"/>.</exception>
    internal static int FindUrlParameter(ParameterInfo[] parameterArray, ParameterAttributeSet[] sets)
    {
        var urlIndex = -1;

        for (var i = 0; i < parameterArray.Length; i++)
        {
            var param = parameterArray[i];
            if (sets[i].Url is null)
            {
                continue;
            }

            if (urlIndex >= 0)
            {
                throw new ArgumentException("Only one parameter can be a [Url] parameter");
            }

            if (param.ParameterType != typeof(string) && param.ParameterType != typeof(Uri))
            {
                throw new ArgumentException(
                    $"[Url] parameter \"{param.Name}\" must be of type string or System.Uri");
            }

            urlIndex = i;
        }

        return urlIndex;
    }
}
