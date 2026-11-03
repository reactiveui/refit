// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Registers the reflection support once so tests that build form maps directly do not depend on test order.</summary>
public static class ReflectionSupportSetup
{
    /// <summary>Registers the reflection form flattener before any test runs.</summary>
    [Before(Assembly)]
    public static void RegisterReflectionSupport() => ReflectionSupport.EnsureRegistered();
}
