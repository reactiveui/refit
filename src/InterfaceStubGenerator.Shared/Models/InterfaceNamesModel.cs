// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes the interface's names metadata.</summary>
/// <param name="GeneratedClassName">The assembly-scoped name of the generated implementation container type.</param>
/// <param name="ClassName">The generated class name.</param>
/// <param name="Ns">The namespace of the interface.</param>
/// <param name="ClassDeclaration">The generated class declaration text.</param>
/// <param name="InterfaceDisplayName">The display name of the interface.</param>
/// <param name="ClassSuffix">The suffix appended to the generated class name.</param>
internal readonly record struct InterfaceNamesModel(
    string GeneratedClassName,
    string ClassName,
    string Ns,
    string ClassDeclaration,
    string InterfaceDisplayName,
    string ClassSuffix);
