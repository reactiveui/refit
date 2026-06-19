// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Reflection;
using System.Runtime.Loader;

namespace Refit.GeneratorTests;

/// <summary>Collectible load context used by live-compilation source-generator tests.</summary>
public sealed class CollectibleAssemblyLoadContext : AssemblyLoadContext, IDisposable
{
    /// <summary>Initializes a new instance of the <see cref="CollectibleAssemblyLoadContext"/> class.</summary>
    public CollectibleAssemblyLoadContext()
        : base(isCollectible: true)
    {
    }

    /// <inheritdoc/>
    public void Dispose() => Unload();

    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName.Name))
        {
            return null;
        }

        foreach (var assembly in Default.Assemblies)
        {
            if (string.Equals(
                assembly.GetName().Name,
                assemblyName.Name,
                StringComparison.OrdinalIgnoreCase))
            {
                return assembly;
            }
        }

        return null;
    }
}
