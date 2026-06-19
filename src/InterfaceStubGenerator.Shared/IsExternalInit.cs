// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

// The IsExternalInit marker is only required on target frameworks that predate the
// init-only setter feature. The OR-list is split across two directives so no single
// preprocessor line exceeds the line-length limit.
#if NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP2_2 || NETCOREAPP3_0
#define REFIT_REQUIRES_ISEXTERNALINIT
#elif NETCOREAPP3_1 || NET45 || NET451 || NET452 || NET6 || NET461 || NET462 || NET47 || NET471 || NET472 || NET48
#define REFIT_REQUIRES_ISEXTERNALINIT
#endif

#if REFIT_REQUIRES_ISEXTERNALINIT

using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>Reserved to be used by the compiler for tracking metadata. This class should not be used by developers in source code.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Maintainability",
    "SST1436:Empty types should not be declared",
    Justification = "Compiler-required init-only marker type; intentionally empty.")]
internal static class IsExternalInit;

#endif
