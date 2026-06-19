// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>An empty custom reference type used to verify Refit's nullable reference handling.</summary>
[SuppressMessage("RoslynCommonAnalyzers", "SST1436", Justification = "Intentional empty fixture type used to verify nullable reference handling in Refit tests.")]
public sealed class CustomReferenceType;
