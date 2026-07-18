// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>
/// Builds the unique generated type name for a Refit interface. This matches the name Refit uses when
/// registering a client with <c>IHttpClientFactory</c>, so it can be used to resolve or configure that
/// same named client (for example <c>services.AddHttpClient(UniqueName.ForType&lt;T&gt;())</c>).
/// </summary>
public static class UniqueName
{
    /// <summary>The initial stack buffer size for sanitizing an assembly name; longer names grow onto pooled storage.</summary>
    private const int AssemblyNameStackBufferLength = 128;

    /// <summary>Builds the unique name for the generated implementation of the given interface type.</summary>
    /// <typeparam name="T">The Refit interface type.</typeparam>
    /// <returns>The unique generated type name.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "T is bound via typeof(T); there is no value argument to infer it from, so callers pass it explicitly.")]
    public static string ForType<T>() => ForType(typeof(T));

    /// <summary>Builds the unique name for the given interface type and service key.</summary>
    /// <typeparam name="T">The Refit interface type.</typeparam>
    /// <param name="serviceKey">The service key to incorporate.</param>
    /// <returns>The unique generated type name.</returns>
    [SuppressMessage(
        "Design",
        "SST2307:Generic method type parameters should be inferable from the parameters",
        Justification = "T is bound via typeof(T); there is no value argument to infer it from, so callers pass it explicitly.")]
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
            interfaceTypeName = interfaceTypeName[(lastDot + 1)..];
        }

        // Now we have the interface name like IFooBar`1[[Some Generic Args]]
        // Or Nested+IFrob
        var genericArgs = string.Empty;

        // if there's any generics, split that
        if (refitInterfaceType.IsGenericType)
        {
            genericArgs = interfaceTypeName[interfaceTypeName.IndexOf('[')..];
            interfaceTypeName = interfaceTypeName[..^genericArgs.Length];
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

        // Refit types are generated as private classes within a container type in namespace Refit.Implementation.
        // The container name folds in the interface's assembly name so each assembly emits a distinctly named
        // container; the generator emits the same name for the same assembly, so this reconstruction matches it.
        // E.g., Refit.Implementation.GeneratedMyApp.NamespaceContainingTheInterfaceType
        var assemblyScope = SanitizeAssemblyName(refitInterfaceType.Assembly.GetName().Name);
        var refitTypeName =
            $"Refit.Implementation.Generated{assemblyScope}+{ns}{interfaceTypeName}{genericArgs}";

        return $"{refitTypeName}, {refitInterfaceType.Assembly.FullName}";
    }

    /// <summary>Reduces an assembly name to an identifier fragment folded into the generated container name.</summary>
    /// <param name="assemblyName">The simple assembly name, or <see langword="null"/> when unavailable.</param>
    /// <returns>The fragment, or an empty string when the assembly name is null or empty. This must stay identical to
    /// the source generator's sanitization so the reconstructed container name matches the emitted one byte-for-byte.</returns>
    internal static string SanitizeAssemblyName(string? assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName))
        {
            return string.Empty;
        }

        // Assembly names routinely contain dots and dashes, which are illegal inside an identifier, so every character
        // that cannot appear in one is folded to an underscore.
        var builder = new ValueStringBuilder(stackalloc char[AssemblyNameStackBufferLength]);
        foreach (var character in assemblyName!)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.ToString();
    }

    /// <summary>Returns the suffix for the service key to be added to the unique name for a given type.</summary>
    /// <param name="serviceKey">The service key to create the suffix from.</param>
    /// <returns>The suffix to be added to the unique name of a given type.</returns>
    private static string GetServiceKeySuffix(object? serviceKey) =>
        serviceKey is null or "" ? string.Empty : $", ServiceKey={serviceKey}";
}
