// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>A non-generic Refit interface fixture that declares generic methods.</summary>
public interface INonGenericInterfaceWithGenericMethod
{
    /// <summary>Posts a message of an arbitrary type.</summary>
    /// <typeparam name="T">The message type to post.</typeparam>
    /// <param name="message">The message to post.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("")]
    Task PostMessage<T>([Body] T message)
        where T : IMessage;

    /// <summary>Posts a message together with two additional generic parameters.</summary>
    /// <typeparam name="T">The message type to post.</typeparam>
    /// <typeparam name="TParam1">A type derived from <typeparamref name="T"/>.</typeparam>
    /// <typeparam name="TParam2">An additional generic parameter type.</typeparam>
    /// <param name="message">The message to post.</param>
    /// <param name="param1">The first additional parameter.</param>
    /// <param name="param2">The second additional parameter.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Post("")]
    Task PostMessage<T, TParam1, TParam2>([Body] T message, TParam1 param1, TParam2 param2)
        where T : IMessage
        where TParam1 : T;
}
