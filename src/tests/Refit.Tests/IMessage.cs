// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>A message interface used as a generic constraint by the generator test fixtures.</summary>
public interface IMessage
{
    /// <summary>Describes a message without affecting its generic constraint identity.</summary>
    /// <returns>The message description.</returns>
    string Describe();
}
