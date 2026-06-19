// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Describes a Refit interface and the data needed to generate its implementation.</summary>
/// <param name="PreserveAttributeDisplayName">The display name of the Preserve attribute.</param>
/// <param name="FileName">The generated file name.</param>
/// <param name="ClassName">The generated class name.</param>
/// <param name="Ns">The namespace of the interface.</param>
/// <param name="ClassDeclaration">The generated class declaration text.</param>
/// <param name="InterfaceDisplayName">The display name of the interface.</param>
/// <param name="ClassSuffix">The suffix appended to the generated class name.</param>
/// <param name="Constraints">The generic type constraints of the interface.</param>
/// <param name="MemberNames">The names of the interface members.</param>
/// <param name="NonRefitMethods">The non-Refit methods declared on the interface.</param>
/// <param name="RefitMethods">The Refit methods declared on the interface.</param>
/// <param name="DerivedRefitMethods">The Refit methods inherited from base interfaces.</param>
/// <param name="Nullability">The nullable reference type context of the interface.</param>
/// <param name="DisposeMethod">A value indicating whether the interface declares a dispose method.</param>
internal sealed record InterfaceModel(
    string PreserveAttributeDisplayName,
    string FileName,
    string ClassName,
    string Ns,
    string ClassDeclaration,
    string InterfaceDisplayName,
    string ClassSuffix,
    ImmutableEquatableArray<TypeConstraint> Constraints,
    ImmutableEquatableArray<string> MemberNames,
    ImmutableEquatableArray<MethodModel> NonRefitMethods,
    ImmutableEquatableArray<MethodModel> RefitMethods,
    ImmutableEquatableArray<MethodModel> DerivedRefitMethods,
    Nullability Nullability,
    bool DisposeMethod);
