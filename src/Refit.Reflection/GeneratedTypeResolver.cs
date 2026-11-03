// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Resolves generated implementation types by their emitted names.</summary>
internal static class GeneratedTypeResolver
{
    /// <summary>Resolves the generated implementation type for a Refit interface by its emitted name.</summary>
    /// <param name="refitInterfaceType">The Refit interface type.</param>
    /// <returns>The generated implementation type.</returns>
    internal static Type Resolve(Type refitInterfaceType) =>
        Type.GetType(UniqueName.ForType(refitInterfaceType), false)
        ?? throw RestService.CreateMissingGeneratedFactoryException(refitInterfaceType);
}
