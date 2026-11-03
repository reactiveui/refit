// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Refit;

/// <summary>Resolves the field name a content serializer gives a property, by reading the attributes its name style uses.</summary>
internal static class SerializerFieldNames
{
    /// <summary>The full name of Newtonsoft.Json's property-name attribute, matched by name so this package needs no Newtonsoft reference.</summary>
    private const string NewtonsoftAttributeName = "Newtonsoft.Json.JsonPropertyAttribute";

    /// <summary>Resolves the serializer-specific field name for a property.</summary>
    /// <param name="serializer">The configured content serializer.</param>
    /// <param name="property">The property to name.</param>
    /// <returns>The serializer's name for the property, or <see langword="null"/> when it has none.</returns>
    internal static string? Resolve(IHttpContentSerializer serializer, PropertyInfo property)
    {
        ArgumentExceptionHelper.ThrowIfNull(property);

        if (serializer is IReflectionFieldNameProvider provider)
        {
            return provider.GetFieldNameForProperty(property);
        }

        return serializer is IContentSerializerNameStyle style
            ? ResolveForStyle(style.NameStyle, property)
            : null;
    }

    /// <summary>Resolves the field name for one attribute family.</summary>
    /// <param name="nameStyle">The attribute family.</param>
    /// <param name="property">The property to name.</param>
    /// <returns>The name, or <see langword="null"/>.</returns>
    internal static string? ResolveForStyle(ContentSerializerNameStyle nameStyle, PropertyInfo property) =>
        nameStyle switch
        {
            ContentSerializerNameStyle.Newtonsoft => ReadNewtonsoftName(property),
            ContentSerializerNameStyle.Xml => property.GetCustomAttribute<XmlElementAttribute>(true)?.ElementName
                ?? property.GetCustomAttribute<XmlAttributeAttribute>(true)?.AttributeName,
            _ => property.GetCustomAttribute<JsonPropertyNameAttribute>(true)?.Name,
        };

    /// <summary>Reads the <c>PropertyName</c> of a Newtonsoft.Json <c>[JsonProperty]</c> attribute by its type name.</summary>
    /// <param name="property">The property to inspect.</param>
    /// <returns>The attribute's property name, or <see langword="null"/>.</returns>
    internal static string? ReadNewtonsoftName(PropertyInfo property)
    {
        foreach (var attribute in property.GetCustomAttributes(true))
        {
            for (var type = attribute.GetType(); type is not null; type = type.BaseType)
            {
                if (type.FullName == NewtonsoftAttributeName)
                {
                    return type.GetProperty("PropertyName")?.GetValue(attribute) as string;
                }
            }
        }

        return null;
    }
}
