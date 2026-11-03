// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes how the generated factory registration is hooked to assembly load in the consumer compilation.</summary>
internal enum ModuleInitializerSupport
{
    /// <summary>The registration is emitted only for targets that define <c>NET5_0_OR_GREATER</c>, because the language or the target cannot run a module initializer.</summary>
    Guarded = 0,

    /// <summary>The registration is always emitted, because the compilation already defines <c>ModuleInitializerAttribute</c>.</summary>
    Unguarded = 1,

    /// <summary>The registration is always emitted and the generator declares <c>ModuleInitializerAttribute</c> for targets that lack it.</summary>
    UnguardedWithPolyfill = 2,
}
