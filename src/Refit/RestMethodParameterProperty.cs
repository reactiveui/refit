// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit;

/// <summary>Describes a property used as part of a REST method parameter.</summary>
/// <remarks>
/// A dotted route placeholder such as <c>{order.customer.id}</c> binds through a chain of properties. The
/// <see cref="PropertyChain"/> is the ordered navigation from the parameter to the bound value, and
/// <see cref="PropertyInfo"/> is its final element (the value formatted into the URL). A single-level binding has a
/// one-element chain, so existing consumers that read <see cref="PropertyInfo"/> behave unchanged.
/// </remarks>
public class RestMethodParameterProperty
{
    /// <summary>Initializes a new instance of the <see cref="RestMethodParameterProperty"/> class for a single property.</summary>
    /// <param name="name">The name.</param>
    /// <param name="propertyInfo">The property information.</param>
    public RestMethodParameterProperty(string name, PropertyInfo propertyInfo)
    {
        Name = name;
        PropertyInfo = propertyInfo;
        PropertyChain = [propertyInfo];
    }

    /// <summary>Initializes a new instance of the <see cref="RestMethodParameterProperty"/> class for a nested property chain.</summary>
    /// <param name="name">The name.</param>
    /// <param name="propertyChain">The ordered navigation from the parameter to the bound value.</param>
    public RestMethodParameterProperty(string name, IReadOnlyList<PropertyInfo> propertyChain)
    {
        Name = name;
        PropertyChain = propertyChain;
        PropertyInfo = propertyChain[propertyChain.Count - 1];
    }

    /// <summary>Gets or sets the property name.</summary>
    public string Name { get; set; }

    /// <summary>Gets or sets the property information for the bound value (the final link of <see cref="PropertyChain"/>).</summary>
    public PropertyInfo PropertyInfo { get; set; }

    /// <summary>Gets or sets the ordered property navigation from the parameter to the bound value.</summary>
    /// <remarks>A single-level binding is a one-element chain; a dotted <c>{a.b.c}</c> binding walks each link in order.</remarks>
    public IReadOnlyList<PropertyInfo> PropertyChain { get; set; }
}
