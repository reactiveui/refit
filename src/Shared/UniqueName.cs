// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>Builds the unique generated type name for a Refit interface.</summary>
internal static class UniqueName
{
    /// <summary>Builds the unique name for the generated implementation of the given interface type.</summary>
    /// <typeparam name="T">The Refit interface type.</typeparam>
    /// <returns>The unique generated type name.</returns>
    [SuppressMessage("Sonar", "S4018", Justification = "T is bound via typeof(T); there is no value argument to infer it from, so callers pass it explicitly.")]
    public static string ForType<T>() => ForType(typeof(T));

    /// <summary>Builds the unique name for the given interface type and service key.</summary>
    /// <typeparam name="T">The Refit interface type.</typeparam>
    /// <param name="serviceKey">The service key to incorporate.</param>
    /// <returns>The unique generated type name.</returns>
    [SuppressMessage("Sonar", "S4018", Justification = "T is bound via typeof(T); there is no value argument to infer it from, so callers pass it explicitly.")]
    public static string ForType<T>(object? serviceKey) => ForType(typeof(T), serviceKey);

    /// <summary>Builds the unique name for the given interface type and service key.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <param name="serviceKey">The service key to incorporate.</param>
    /// <returns>The unique generated type name.</returns>
    public static string ForType(Type refitInterfaceType, object? serviceKey) =>
        ForType(refitInterfaceType) + GetServiceKeySuffix(serviceKey);

    /// <summary>Builds the unique name for the given interface type.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <returns>The unique generated type name.</returns>
    public static string ForType(Type refitInterfaceType)
    {
        var interfaceTypeName = refitInterfaceType.FullName!;

        // remove namespace/nested, up to anything before a `
        var searchEnd = interfaceTypeName.IndexOf('`');
        if (searchEnd < 0)
        {
            searchEnd = interfaceTypeName.Length;
        }

        var lastDot = interfaceTypeName.LastIndexOf('.', searchEnd - 1);
        if (lastDot > 0)
        {
            interfaceTypeName = interfaceTypeName.Substring(lastDot + 1);
        }

        // Now we have the interface name like IFooBar`1[[Some Generic Args]]
        // Or Nested+IFrob
        var genericArgs = string.Empty;

        // if there's any generics, split that
        if (refitInterfaceType.IsGenericType)
        {
            genericArgs = interfaceTypeName.Substring(interfaceTypeName.IndexOf('['));
            interfaceTypeName = interfaceTypeName.Substring(
                0,
                interfaceTypeName.Length - genericArgs.Length);
        }

        // Remove any + from the type name portion
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
        interfaceTypeName = interfaceTypeName.Replace("+", string.Empty, StringComparison.Ordinal);

        // Get the namespace and remove the dots
        var ns = refitInterfaceType.Namespace?.Replace(".", string.Empty, StringComparison.Ordinal);
#else
        interfaceTypeName = interfaceTypeName.Replace("+", string.Empty);

        // Get the namespace and remove the dots
        var ns = refitInterfaceType.Namespace?.Replace(".", string.Empty);
#endif

        // Refit types will be generated as private classes within a Generated type in namespace
        // Refit.Implementation
        // E.g., Refit.Implementation.Generated.NamespaceContainingTpeInterfaceType
        var refitTypeName =
            $"Refit.Implementation.Generated+{ns}{interfaceTypeName}{genericArgs}";

        return $"{refitTypeName}, {refitInterfaceType.Assembly.FullName}";
    }

    /// <summary>Returns the suffix for the service key to be added to the unique name for a given type.</summary>
    /// <param name="serviceKey">The service key to create the suffix from.</param>
    /// <returns>The suffix to be added to the unique name of a given type.</returns>
    private static string GetServiceKeySuffix(object? serviceKey) =>
        serviceKey is null or "" ? string.Empty : $", ServiceKey={serviceKey}";
}
