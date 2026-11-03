// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Refit.Testing.Tests;

/// <summary>Registers reflection-based JSON for the tests that stub typed replies without a source-generated context.</summary>
internal static class ReflectionRegistration
{
    /// <summary>Registers the reflection support once, before any test runs.</summary>
    [ModuleInitializer]
    internal static void Register() => ReflectionSupport.EnsureRegistered();
}
