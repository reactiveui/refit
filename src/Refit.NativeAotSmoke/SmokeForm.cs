// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.NativeAotSmoke;

/// <summary>The URL-encoded form payload used by the native AOT smoke test.</summary>
/// <param name="Name">The form name.</param>
/// <param name="Count">The form count.</param>
public sealed record SmokeForm(string Name, int Count);
