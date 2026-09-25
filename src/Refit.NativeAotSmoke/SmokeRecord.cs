// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.NativeAotSmoke;

/// <summary>A record uploaded through the native AOT smoke test's typed JSON Lines upload.</summary>
/// <param name="Sequence">The record's position in the produced sequence.</param>
/// <param name="Label">A label identifying the record.</param>
[System.Diagnostics.DebuggerDisplay("{Sequence}: {Label}")]
public sealed record SmokeRecord(int Sequence, string Label);
