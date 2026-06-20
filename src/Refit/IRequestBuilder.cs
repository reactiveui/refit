// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>
/// Defines a method for creating a delegate that executes a REST API method with the specified parameters and
/// returns the result.
/// </summary>
/// <remarks>The returned delegate can be used to invoke a REST API method dynamically, given an HTTP
/// client and an array of arguments. This interface is typically used by code generation or dynamic proxy libraries
/// to construct method invokers at runtime. Implementations may use reflection and may require referenced types to
/// be preserved when trimming assemblies.</remarks>
public interface IRequestBuilder
{
    /// <summary>Gets the settings used by this request builder.</summary>
    RefitSettings Settings { get; }

    /// <summary>Builds a delegate that executes the specified REST method using the provided HTTP client and arguments.</summary>
    /// <remarks>The returned delegate uses reflection to invoke the specified method and may require
    /// referenced interfaces and data transfer objects (DTOs) to be preserved when trimming assemblies. This method
    /// is typically used to dynamically generate REST API client calls at runtime.</remarks>
    /// <param name="methodName">The name of the interface method to generate the REST call delegate for. Must correspond to a method defined
    /// on the target interface.</param>
    /// <param name="parameterTypes">An array of parameter types for the target method, or null to infer from the method signature. The order
    /// must match the method's parameter list if specified.</param>
    /// <param name="genericArgumentTypes">An array of generic argument types to use when constructing a generic method, or null if the method is not
    /// generic.</param>
    /// <returns>A delegate that takes an HttpClient and an array of argument values, and returns the result of invoking the
    /// specified REST method. The return type matches the method's declared return type.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S2360:Optional parameters should not be used",
        Justification = "The optional parameters are part of Refit's public request-builder contract and are relied on by the Refit source generator and existing callers.")]
    [RequiresUnreferencedCode(
        "Refit uses reflection to analyze interface methods. Ensure referenced interfaces and DTOs are preserved when trimming.")]
    [RequiresDynamicCode("Refit may generate or invoke code dynamically for this path.")]
    Func<HttpClient, object[], object?> BuildRestResultFuncForMethod(
        string methodName,
        Type[]? parameterTypes = null,
        Type[]? genericArgumentTypes = null);
}
