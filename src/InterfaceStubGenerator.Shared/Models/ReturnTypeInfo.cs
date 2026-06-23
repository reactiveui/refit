// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Describes the shape of a method's return type.</summary>
internal enum ReturnTypeInfo
{
    /// <summary>The method returns a value synchronously.</summary>
    Return,

    /// <summary>The method returns an awaitable with no result.</summary>
    AsyncVoid,

    /// <summary>The method returns an awaitable with a result.</summary>
    AsyncResult,

    /// <summary>The method returns an IAsyncEnumerable stream.</summary>
    AsyncEnumerable,

    /// <summary>The method returns void synchronously.</summary>
    SyncVoid
}
