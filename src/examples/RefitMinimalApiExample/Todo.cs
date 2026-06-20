// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.MinimalApi.Example;

/// <summary>An item returned by the example API.</summary>
/// <param name="Id">The item identifier.</param>
/// <param name="Title">The item title.</param>
public sealed record Todo(int Id, string Title);
