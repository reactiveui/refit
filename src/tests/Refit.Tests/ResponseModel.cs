// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests.SeparateNamespaceWithModel;

/// <summary>An intentionally empty response model in a separate namespace, used to verify the generator emits the required using directive.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "RoslynCommonAnalyzers",
    "SST1436:Add members to a type or remove it",
    Justification = "Intentional empty response fixture used to verify generated using directives without changing the public shape.")]
public class ResponseModel;
