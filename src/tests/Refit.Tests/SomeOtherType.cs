// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests.Common;

/// <summary>Empty response type used to verify namespace overlap deserialization in Refit.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "RoslynCommonAnalyzers",
    "SST1436:Add members to a type or remove it",
    Justification = "Intentional empty response fixture used to verify namespace overlap handling without changing the public shape.")]
public class SomeOtherType;
