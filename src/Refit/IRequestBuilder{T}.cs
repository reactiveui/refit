// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>Defines a generic contract for building requests with a specified result type.</summary>
/// <typeparam name="T">The type of the result produced by the request builder.</typeparam>
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "The type parameter identifies the Refit interface and is intentionally carried by this marker interface for strongly typed APIs.")]
[SuppressMessage(
    "StyleSharp",
    "SST1452:Unused type parameters should be removed",
    Justification = "The type parameter identifies the Refit interface and is intentionally carried by this marker interface for strongly typed APIs.")]
public interface IRequestBuilder<T> : IRequestBuilder;
