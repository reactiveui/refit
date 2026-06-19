// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>A base record exposing a single value used as a URL parameter source.</summary>
/// <param name="Value">The value carried by the record.</param>
[SuppressMessage("RoslynCommonAnalyzers", "SST1800:Seal record or make it abstract", Justification = "Polymorphic base record that is both instantiated directly and inherited by test fixtures.")]
public record BaseRecord(string Value);
