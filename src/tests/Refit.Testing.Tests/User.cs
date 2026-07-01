// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Testing.Tests;

/// <summary>A sample response body.</summary>
/// <param name="Id">The user identifier.</param>
/// <param name="Login">The user login.</param>
public sealed record User(int Id, string Login);
