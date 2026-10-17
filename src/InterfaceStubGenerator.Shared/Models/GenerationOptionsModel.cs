// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes the consumer's generated source options.</summary>
/// <param name="PreserveAttributeDisplayName">The display name of the generated preserve attribute.</param>
/// <param name="GeneratedClassName">The assembly-scoped name of the generated implementation container type.</param>
/// <param name="GeneratedRequestBuilding">Whether generated request construction is enabled.</param>
/// <param name="EmitGeneratedCodeMarkers">Whether generated files include generated-code analyzer skip markers.</param>
internal readonly record struct GenerationOptionsModel(
    string PreserveAttributeDisplayName,
    string GeneratedClassName,
    bool GeneratedRequestBuilding,
    bool EmitGeneratedCodeMarkers);
