// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Model describing the shared generation context for a set of Refit interfaces.</summary>
/// <param name="RefitInternalNamespace">The namespace used for Refit internal generated types.</param>
/// <param name="PreserveAttributeDisplayName">The display name of the preserve attribute.</param>
/// <param name="GeneratedClassName">The assembly-scoped name of the generated implementation container type.</param>
/// <param name="GeneratedRequestBuilding">Whether generated request construction is enabled.</param>
/// <param name="EmitGeneratedCodeMarkers">Whether generated files include generated-code analyzer skip markers.</param>
/// <param name="Interfaces">The interfaces to generate implementations for.</param>
internal sealed record ContextGenerationModel(
    string RefitInternalNamespace,
    string PreserveAttributeDisplayName,
    string GeneratedClassName,
    bool GeneratedRequestBuilding,
    bool EmitGeneratedCodeMarkers,
    ImmutableEquatableArray<InterfaceModel> Interfaces);
