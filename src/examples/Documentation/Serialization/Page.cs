// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Documentation;

/// <summary>Groups items in a generic model requiring its own closed-type JSON registration.</summary>
/// <typeparam name="T">The model type of each page item.</typeparam>
/// <param name="Items">The page's serialized item array.</param>
internal sealed record Page<T>(T[] Items);
