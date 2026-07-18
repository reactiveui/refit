// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using BenchmarkDotNet.Running;

// The generator benchmarks are always selected by filter (there is no single "default" suite),
// so route every invocation through the switcher.
_ = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

/// <summary>The generated top-level program's declaring type, sealed so the JIT can devirtualize its members.</summary>
internal sealed partial class Program
{
    /// <summary>Initializes a new instance of the <see cref="Program"/> class. Unused; the entry point is the generated top-level <c>Main</c>.</summary>
    private Program()
    {
    }
}
