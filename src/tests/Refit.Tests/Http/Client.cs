// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests.Http;

/// <summary>An HTTP client fixture used to verify unique-name generation across namespaces and nested types.</summary>
public sealed class Client
{
    /// <summary>A request fixture nested inside <see cref="Client"/> used to verify unique-name generation for nested types.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "RoslynCommonAnalyzers",
        "SST1436:Add members to a type or remove it",
        Justification = "Intentional empty nested fixture type used to verify unique-name generation.")]
    public sealed class Request;

    /// <summary>A response fixture nested inside <see cref="Client"/> used to verify unique-name generation for nested types.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "RoslynCommonAnalyzers",
        "SST1436:Add members to a type or remove it",
        Justification = "Intentional empty nested fixture type used to verify unique-name generation.")]
    public sealed class Response;
}
