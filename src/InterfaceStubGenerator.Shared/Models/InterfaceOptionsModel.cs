// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes the interface's options metadata.</summary>
/// <param name="PreserveAttributeDisplayName">The display name of the Preserve attribute.</param>
/// <param name="GeneratedRequestBuilding">Whether generated request construction is enabled for this interface.</param>
/// <param name="EmitGeneratedCodeMarkers">Whether generated files include generated-code analyzer skip markers.</param>
/// <param name="SupportsNullable">Whether the consumer compilation supports nullable reference type syntax.</param>
/// <param name="SupportsStaticLambdas">Whether the consumer compilation supports the <c>static</c> lambda modifier (C# 9).</param>
/// <param name="SupportsCollectionExpressions">Whether the consumer compilation supports collection expressions (C# 12),
/// so path replacements can be passed as a stack-allocatable <c>[...]</c> span instead of an array.</param>
internal readonly record struct InterfaceOptionsModel(
    string PreserveAttributeDisplayName,
    bool GeneratedRequestBuilding,
    bool EmitGeneratedCodeMarkers,
    bool SupportsNullable,
    bool SupportsStaticLambdas,
    bool SupportsCollectionExpressions);
