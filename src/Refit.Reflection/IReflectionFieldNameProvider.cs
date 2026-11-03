// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit;

/// <summary>
/// Implemented by a custom content serializer to name form and query fields from the attributes it understands.
/// The built-in System.Text.Json, Newtonsoft.Json and XML serializers need no implementation: Refit reads their
/// attributes itself.
/// </summary>
public interface IReflectionFieldNameProvider
{
    /// <summary>
    /// Calculates what the field name should be for the given property. This may be affected by custom attributes the serializer understands.
    /// </summary>
    /// <param name="propertyInfo">A PropertyInfo object.</param>
    /// <returns>The calculated field name, or <see langword="null"/> to use the default.</returns>
    string? GetFieldNameForProperty(PropertyInfo propertyInfo);
}
