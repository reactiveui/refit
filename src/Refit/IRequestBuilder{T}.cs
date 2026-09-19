// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit;

/// <summary>Identifies the API interface targeted by a request builder.</summary>
/// <typeparam name="T">The Refit API interface.</typeparam>
[SuppressMessage(
    "StyleSharp",
    "SST1452:Unused type parameters should be removed",
    Justification = "The type parameter identifies the Refit interface and is intentionally carried by this marker interface for strongly typed APIs.")]
public interface IRequestBuilder<T> : IRequestBuilder;
