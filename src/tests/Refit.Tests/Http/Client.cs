// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests.Http;

/// <summary>An HTTP client fixture used to verify unique-name generation across namespaces and nested types.</summary>
[SuppressMessage("Design", "SST1436:Add members to type or remove it", Justification = "Intentional empty type marker; used only as a generic type argument to UniqueName.ForType.")]
public sealed class Client
{
    /// <summary>A request fixture nested inside <see cref="Client"/> used to verify unique-name generation for nested types.</summary>
    [SuppressMessage("Design", "SST1436:Add members to type or remove it", Justification = "Intentional empty type marker; used only as a generic type argument to UniqueName.ForType.")]
    public sealed class Request;

    /// <summary>A response fixture nested inside <see cref="Client"/> used to verify unique-name generation for nested types.</summary>
    [SuppressMessage("Design", "SST1436:Add members to type or remove it", Justification = "Intentional empty type marker; used only as a generic type argument to UniqueName.ForType.")]
    public sealed class Response;
}
