// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Typed caching request builder that targets a specific Refit interface.</summary>
/// <typeparam name="T">The Refit interface type requests are built for.</typeparam>
internal class CachedRequestBuilderImplementation<T>
    : CachedRequestBuilderImplementation,
        IRequestBuilder<T>
{
    /// <summary>Initializes a new instance of the <see cref="CachedRequestBuilderImplementation{T}"/> class.</summary>
    /// <param name="innerBuilder">The typed request builder whose results are cached.</param>
    public CachedRequestBuilderImplementation(IRequestBuilder<T> innerBuilder)
        : base(innerBuilder)
    {
    }
}
