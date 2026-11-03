// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Exposes the attributes declared on a request parameter or property, as precomputed by the generator or read by Refit.Reflection.</summary>
public interface IParameterAttributeProvider
{
    /// <summary>Gets all the attributes declared on the parameter or property.</summary>
    /// <param name="inherit">Whether to search the inheritance chain.</param>
    /// <returns>The attributes, or an empty array.</returns>
    object[] GetCustomAttributes(bool inherit);

    /// <summary>Gets the attributes of the given type declared on the parameter or property.</summary>
    /// <param name="attributeType">The attribute type to look for.</param>
    /// <param name="inherit">Whether to search the inheritance chain.</param>
    /// <returns>The matching attributes, or an empty array.</returns>
    object[] GetCustomAttributes(Type attributeType, bool inherit);

    /// <summary>Determines whether an attribute of the given type is declared on the parameter or property.</summary>
    /// <param name="attributeType">The attribute type to look for.</param>
    /// <param name="inherit">Whether to search the inheritance chain.</param>
    /// <returns><see langword="true"/> when at least one matching attribute is declared.</returns>
    bool IsDefined(Type attributeType, bool inherit);
}
