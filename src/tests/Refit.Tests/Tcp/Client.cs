// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests.Tcp;

/// <summary>
/// A TCP client fixture used to verify that <see cref="T:Refit.UniqueName"/> produces a name
/// distinct from the identically named client in the <see cref="Http"/> namespace.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "RoslynCommonAnalyzers",
    "SST1436:Add members to a type or remove it",
    Justification = "Intentional empty fixture type used to verify unique-name generation across namespaces.")]
public sealed class Client;
