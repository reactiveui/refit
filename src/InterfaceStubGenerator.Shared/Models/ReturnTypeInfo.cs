// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Describes the shape of a method's return type.</summary>
internal enum ReturnTypeInfo
{
    /// <summary>The method returns a value synchronously.</summary>
    Return = 0,

    /// <summary>The method returns an awaitable with no result.</summary>
    AsyncVoid = 1,

    /// <summary>The method returns an awaitable with a result.</summary>
    AsyncResult = 2,

    /// <summary>The method returns an IAsyncEnumerable stream.</summary>
    AsyncEnumerable = 3,

    /// <summary>The method returns an <c>IObservable&lt;T&gt;</c> (a cold observable that sends per subscription).</summary>
    Observable = 4,

    /// <summary>The method returns void synchronously.</summary>
    SyncVoid = 5,

    /// <summary>The method returns the built <c>Task&lt;HttpRequestMessage&gt;</c> without sending it.</summary>
    RequestMessage = 6,
}
