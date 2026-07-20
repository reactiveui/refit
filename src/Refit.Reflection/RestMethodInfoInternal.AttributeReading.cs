// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit;

/// <summary>Per-parameter attribute reading for <see cref="RestMethodInfoInternal"/>. Reads each parameter's
/// request-shaping attributes once into a classified set so the constructor never re-enumerates a parameter's attribute
/// records for each distinct lookup.</summary>
internal partial class RestMethodInfoInternal
{
    /// <summary>Reads every parameter's request-shaping attributes in a single metadata pass.</summary>
    /// <param name="parameterArray">The parameters excluding cancellation tokens.</param>
    /// <returns>The classified attribute set for each parameter, indexed to match <paramref name="parameterArray"/>.</returns>
    /// <remarks>Each parameter's attributes are enumerated once and sorted by type; a per-request-shaping <c>GetCustomAttribute</c>
    /// call would otherwise re-scan the same attribute records once per lookup, allocating a scratch array each time.</remarks>
    internal static ParameterAttributeSet[] BuildParameterAttributeSets(ParameterInfo[] parameterArray)
    {
        if (parameterArray.Length == 0)
        {
            return [];
        }

        var sets = new ParameterAttributeSet[parameterArray.Length];
        for (var i = 0; i < parameterArray.Length; i++)
        {
            sets[i] = ClassifyParameterAttributes(parameterArray[i]);
        }

        return sets;
    }

    /// <summary>Reads and classifies a single parameter's request-shaping attributes in one metadata pass.</summary>
    /// <param name="parameter">The parameter whose attributes are read.</param>
    /// <returns>The classified attribute set for the parameter.</returns>
    internal static ParameterAttributeSet ClassifyParameterAttributes(ParameterInfo parameter)
    {
        QueryAttribute? query = null;
        HeaderAttribute? header = null;
        HeaderCollectionAttribute? headerCollection = null;
        PropertyAttribute? property = null;
        AuthorizeAttribute? authorize = null;
        BodyAttribute? body = null;
        UrlAttribute? url = null;
        FormObjectAttribute? formObject = null;

        // Each of these attributes is single-per-parameter, so classifying with a null-coalescing assignment keeps the
        // first (only) match of each type without the line or branch cost of a braced type switch.
        foreach (var attribute in parameter.GetCustomAttributes(true))
        {
            query ??= attribute as QueryAttribute;
            header ??= attribute as HeaderAttribute;
            headerCollection ??= attribute as HeaderCollectionAttribute;
            property ??= attribute as PropertyAttribute;
            authorize ??= attribute as AuthorizeAttribute;
            body ??= attribute as BodyAttribute;
            url ??= attribute as UrlAttribute;
            formObject ??= attribute as FormObjectAttribute;
        }

        return new(query, header, headerCollection, property, authorize, body, url, formObject);
    }

    /// <summary>Materializes each parameter's <see cref="QueryAttribute"/> so the per-request mapping path can look it up
    /// from an array instead of re-reading it from metadata on every call.</summary>
    /// <param name="sets">The classified attribute set for each parameter.</param>
    /// <returns>The query attribute for each parameter, or null where absent, indexed to match <paramref name="sets"/>.</returns>
    internal static QueryAttribute?[] BuildParameterQueryAttributes(ParameterAttributeSet[] sets)
    {
        if (sets.Length == 0)
        {
            return [];
        }

        var attributes = new QueryAttribute?[sets.Length];
        for (var i = 0; i < sets.Length; i++)
        {
            attributes[i] = sets[i].Query;
        }

        return attributes;
    }

    /// <summary>Records which parameters carry <see cref="FormObjectAttribute"/> so the per-part multipart path looks the
    /// fact up from an array instead of re-reading it from metadata on every part.</summary>
    /// <param name="sets">The classified attribute set for each parameter.</param>
    /// <param name="isMultipart">Whether the method is multipart; only then is the form-object fact ever consulted.</param>
    /// <returns>A flag per parameter for a multipart method, or a shared empty array otherwise.</returns>
    internal static bool[] BuildFormObjectFlags(ParameterAttributeSet[] sets, bool isMultipart)
    {
        if (!isMultipart)
        {
            return [];
        }

        var flags = new bool[sets.Length];
        for (var i = 0; i < sets.Length; i++)
        {
            flags[i] = sets[i].FormObject is not null;
        }

        return flags;
    }

    /// <summary>Finds the index of the header collection parameter in the parameter array.</summary>
    /// <param name="parameterArray">The array of method parameters.</param>
    /// <param name="sets">The classified attribute set for each parameter.</param>
    /// <returns>The index of the header collection parameter, or a negative value when none exists.</returns>
    internal static int GetHeaderCollectionParameterIndex(ParameterInfo[] parameterArray, ParameterAttributeSet[] sets)
    {
        var headerIndex = -1;

        for (var i = 0; i < parameterArray.Length; i++)
        {
            if (sets[i].HeaderCollection is null)
            {
                continue;
            }

            // Opted for IDictionary<string, string> semantics here as opposed to the looser
            // IEnumerable<KeyValuePair<string, string>> because IDictionary enforces unique keys.
            if (!parameterArray[i].ParameterType.IsAssignableFrom(typeof(IDictionary<string, string>)))
            {
                throw new ArgumentException(
                    $"HeaderCollection parameter of type {parameterArray[i].ParameterType.Name} is not assignable from IDictionary<string, string>");
            }

            // Throw if there is already a HeaderCollection parameter.
            if (headerIndex >= 0)
            {
                throw new ArgumentException("Only one parameter can be a HeaderCollection parameter");
            }

            headerIndex = i;
        }

        return headerIndex;
    }

    /// <summary>Builds the map of parameter indexes to request property keys.</summary>
    /// <param name="parameterArray">The array of method parameters.</param>
    /// <param name="sets">The classified attribute set for each parameter.</param>
    /// <returns>A map of parameter indexes to property keys.</returns>
    internal static Dictionary<int, string> BuildRequestPropertyMap(ParameterInfo[] parameterArray, ParameterAttributeSet[] sets)
    {
        Dictionary<int, string>? propertyMap = null;

        for (var i = 0; i < parameterArray.Length; i++)
        {
            var propertyAttribute = sets[i].Property;
            if (propertyAttribute is not null)
            {
                var propertyKey = !string.IsNullOrEmpty(propertyAttribute.Key)
                    ? propertyAttribute.Key
                    : parameterArray[i].Name!;
                propertyMap ??= [];
                propertyMap[i] = propertyKey!;
            }
        }

        return propertyMap ?? EmptyDictionary<int, string>.Get();
    }

    /// <summary>Builds the map of parameter indexes to header names.</summary>
    /// <param name="sets">The classified attribute set for each parameter.</param>
    /// <returns>A map of parameter indexes to header names.</returns>
    internal static Dictionary<int, string> BuildHeaderParameterMap(ParameterAttributeSet[] sets)
    {
        Dictionary<int, string>? ret = null;

        for (var i = 0; i < sets.Length; i++)
        {
            var header = sets[i].Header?.Header;

            if (!string.IsNullOrWhiteSpace(header))
            {
                ret ??= [];
                ret[i] = header!.Trim();
            }
        }

        return ret ?? EmptyDictionary<int, string>.Get();
    }

    /// <summary>Scans the parameters for an explicit <see cref="BodyAttribute"/>.</summary>
    /// <param name="sets">The classified attribute set for each parameter.</param>
    /// <returns>The first body attribute and its index, and whether more than one parameter carries one.</returns>
    internal static (BodyAttribute? Attribute, int Index, bool HasMultiple) FindBodyAttribute(ParameterAttributeSet[] sets)
    {
        BodyAttribute? bodyAttribute = null;
        var bodyParameterIndex = -1;
        for (var i = 0; i < sets.Length; i++)
        {
            var attribute = sets[i].Body;
            if (attribute is null)
            {
                continue;
            }

            if (bodyAttribute is not null)
            {
                return (bodyAttribute, bodyParameterIndex, true);
            }

            bodyAttribute = attribute;
            bodyParameterIndex = i;
        }

        return (bodyAttribute, bodyParameterIndex, false);
    }

    /// <summary>Finds the parameter that carries the authorization value.</summary>
    /// <param name="sets">The classified attribute set for each parameter.</param>
    /// <returns>The authorization parameter information, or null when there is no authorize parameter.</returns>
    internal static Tuple<string, int>? FindAuthorizationParameter(ParameterAttributeSet[] sets)
    {
        AuthorizeAttribute? authorizeAttribute = null;
        var authorizeIndex = -1;

        for (var i = 0; i < sets.Length; i++)
        {
            var attribute = sets[i].Authorize;
            if (attribute is null)
            {
                continue;
            }

            if (authorizeAttribute is not null)
            {
                throw new ArgumentException("Only one parameter can be an Authorize parameter");
            }

            authorizeAttribute = attribute;
            authorizeIndex = i;
        }

        return authorizeAttribute is null
            ? null
            : Tuple.Create(authorizeAttribute.Scheme, authorizeIndex);
    }
}
