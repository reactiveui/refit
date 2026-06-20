// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests.ModelNamespace;

/// <summary>Empty response type used to verify Refit handling of reduced usings inside a namespace.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "RoslynCommonAnalyzers",
    "SST1436:Add members to a type or remove it",
    Justification = "Intentional empty response fixture used to verify reduced using handling inside a namespace.")]
public class SomeType;
