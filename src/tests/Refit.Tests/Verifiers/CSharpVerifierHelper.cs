// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Tests;

/// <summary>Helpers for configuring analyzer and source-generator test verifiers.</summary>
internal static class CSharpVerifierHelper
{
    /// <summary>
    /// Gets the compiler nullable diagnostic IDs mapped to <see cref="ReportDiagnostic.Error"/>.
    /// By default, the compiler reports diagnostics for nullable reference types at
    /// <see cref="DiagnosticSeverity.Warning"/>, and the analyzer test framework defaults to only validating
    /// diagnostics at <see cref="DiagnosticSeverity.Error"/>. This map enables all nullability warnings for
    /// default validation during analyzer and code fix tests.
    /// </summary>
    internal static ImmutableDictionary<string, ReportDiagnostic> NullableWarnings { get; } =
        GetNullableWarningsFromCompiler();

    /// <summary>Parses the compiler nullable diagnostic options and maps them to errors.</summary>
    /// <returns>A map of nullability diagnostic IDs to <see cref="ReportDiagnostic.Error"/>.</returns>
    private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
    {
        string[] args = ["/warnaserror:nullable"];
        var commandLineArguments = CSharpCommandLineParser.Default.Parse(
            args,
            baseDirectory: Environment.CurrentDirectory,
            sdkDirectory: Environment.CurrentDirectory);
        return commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;
    }
}
