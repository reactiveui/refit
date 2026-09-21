// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.NativeAotSmoke;

/// <summary>A reply type the smoke test's JSON context does not describe, proving reflection-based JSON is disabled.</summary>
/// <param name="Name">The reply name.</param>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public sealed record SmokeUnregistered(string Name);
