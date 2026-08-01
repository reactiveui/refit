// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.NetFrameworkSmoke;

/// <summary>A todo item exchanged with the smoke test endpoint.</summary>
/// <param name="Id">The todo identifier.</param>
/// <param name="Title">The todo title.</param>
internal sealed record Todo(int Id, string Title);
