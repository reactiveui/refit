// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Represents a single fragment of a URL path, either a constant string or a dynamic value.</summary>
/// <param name="Value">The constant value of the fragment, or null when the fragment is dynamic.</param>
/// <param name="ArgumentIndex">The parameter index supplying the dynamic value, or a negative value when constant.</param>
/// <param name="PropertyIndex">The property index within the parameter, or a negative value when not an object property.</param>
/// <param name="IsOptional">Whether the fragment came from an optional <c>{name?}</c> placeholder, so a null value drops
/// the segment and its preceding separator instead of formatting to an empty segment.</param>
internal readonly record struct ParameterFragment(string? Value, int ArgumentIndex, int PropertyIndex, bool IsOptional = false)
{
    /// <summary>Gets a value indicating whether the fragment is a constant string.</summary>
    public bool IsConstant => Value is not null;

    /// <summary>Gets a value indicating whether the fragment is a dynamic route value.</summary>
    public bool IsDynamicRoute => ArgumentIndex >= 0 && PropertyIndex < 0;

    /// <summary>Gets a value indicating whether the fragment is a property of a parameter object.</summary>
    public bool IsObjectProperty => ArgumentIndex >= 0 && PropertyIndex >= 0;

    /// <summary>Creates a constant URL fragment.</summary>
    /// <param name="value">The constant string value.</param>
    /// <returns>A constant fragment.</returns>
    public static ParameterFragment Constant(string value) => new(value, -1, -1);

    /// <summary>Creates a dynamic route fragment bound to a parameter.</summary>
    /// <param name="index">The parameter index supplying the value.</param>
    /// <param name="isOptional">Whether the placeholder was declared optional with the <c>{name?}</c> syntax.</param>
    /// <returns>A dynamic route fragment.</returns>
    public static ParameterFragment Dynamic(int index, bool isOptional = false) => new(null, index, -1, isOptional);

    /// <summary>Creates a dynamic fragment bound to a property of a parameter object.</summary>
    /// <param name="index">The parameter index supplying the object.</param>
    /// <param name="propertyIndex">The property index within the parameter object.</param>
    /// <param name="isOptional">Whether the placeholder was declared optional with the <c>{name?}</c> syntax.</param>
    /// <returns>A dynamic object property fragment.</returns>
    public static ParameterFragment DynamicObject(int index, int propertyIndex, bool isOptional = false) =>
        new(null, index, propertyIndex, isOptional);
}
