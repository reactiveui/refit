// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Refit;

/// <summary>Provides shared helpers for reflected property enumeration.</summary>
internal static class ReflectionPropertyHelpers
{
    /// <summary>Gets the readable public instance properties of the given type.</summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The readable public instance properties.</returns>
    internal static PropertyInfo[] GetReadablePublicInstanceProperties(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        Type type)
    {
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var count = 0;
        for (var i = 0; i < properties.Length; i++)
        {
            if (IsReadablePublicProperty(properties[i]))
            {
                count++;
            }
        }

        if (count == properties.Length)
        {
            return properties;
        }

        var readableProperties = new PropertyInfo[count];
        var index = 0;
        for (var i = 0; i < properties.Length; i++)
        {
            if (IsReadablePublicProperty(properties[i]))
            {
                readableProperties[index] = properties[i];
                index++;
            }
        }

        return readableProperties;
    }

    /// <summary>Determines whether a property can be read through its public getter.</summary>
    /// <param name="property">The property to inspect.</param>
    /// <returns><see langword="true"/> when the property is readable; otherwise <see langword="false"/>.</returns>
    internal static bool IsReadablePublicProperty(PropertyInfo property) =>
        property.CanRead && property.GetMethod!.IsPublic; // A readable property (CanRead) always has a non-null GetMethod.
}
