// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit;

/// <summary>Describes a property used as part of a REST method parameter.</summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RestMethodParameterProperty"/> class.
/// </remarks>
/// <param name="name">The name.</param>
/// <param name="propertyInfo">The property information.</param>
public class RestMethodParameterProperty(string name, PropertyInfo propertyInfo)
{
    /// <summary>Gets or sets the property name.</summary>
    public string Name { get; set; } = name;

    /// <summary>Gets or sets the property information.</summary>
    public PropertyInfo PropertyInfo { get; set; } = propertyInfo;
}
