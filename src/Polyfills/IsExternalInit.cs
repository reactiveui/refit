// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET6_0_OR_GREATER
namespace System.Runtime.CompilerServices;

/// <summary>Polyfill marker enabling init-only setters on older target frameworks.</summary>
[Diagnostics.CodeAnalysis.SuppressMessage("RoslynCommonAnalyzers", "SST1436:Add members to a type or remove it", Justification = "Compiler-required marker type; intentionally empty.")]
internal static class IsExternalInit;
#endif
