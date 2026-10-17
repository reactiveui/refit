// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Analyzers.Tests;

/// <summary>Checks the opt-in warning for delimiters mistaken for query aliases.</summary>
public sealed class QueryDelimiterDiagnosticsTests
{
    /// <summary>The opt-in query delimiter rule.</summary>
    private const string Rule = "RF016";

    /// <summary>Checks scalar misuse and identifier-shaped delimiters without flagging explicit names.</summary>
    /// <param name="parameter">The annotated parameter declaration.</param>
    /// <param name="expected">Whether the warning is expected.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the test.</returns>
    [Test]
    [Arguments("[Query(\"client_id\")] string clientId", true)]
    [Arguments("[Query(\".\")] int value", true)]
    [Arguments("[Query(\"api-version\")] Guid value", true)]
    [Arguments("[Query(\"client_id\")] string[] values", true)]
    [Arguments("[Query(\".\")] string[] values", false)]
    [Arguments("[Query(\"__\")] string[] values", false)]
    [Arguments("[Query(\"foo.bar\")] string[] values", false)]
    [Arguments("[Query(\"123\")] string[] values", false)]
    [Arguments("[Query(\".\")] Dictionary<string, string> values", false)]
    [Arguments("[Query(\"\")] string value", false)]
    [Arguments("[Query] string value", false)]
    [Arguments("[Query(CollectionFormat.Csv)] string[] values", false)]
    [Arguments("[Query(\".\", \"prefix\")] string value", false)]
    [Arguments("[Query(\"client_id\")][AliasAs(\"id\")] string value", false)]
    [Arguments("[Query(\"client_id\", Name = \"id\")] string value", false)]
    [Arguments("[Query(\"client_id\", Name = null)] string value", true)]
    public async Task Analyze_QueryDelimiterMistakes(string parameter, bool expected, CancellationToken cancellationToken)
    {
        var diagnostics = await AnalyzerFixture.RunForBodyWithDiagnostic(
            $"[Get(\"/items\")] Task<string> Get({parameter});",
            Rule,
            cancellationToken);
        var warnings = diagnostics.Where(static diagnostic => diagnostic.Id == Rule).ToArray();
        await Assert.That(warnings.Length).IsEqualTo(expected ? 1 : 0);
        if (expected)
        {
            await Assert.That(warnings[0].GetMessage()).Contains("Query(Name");
        }
    }

    /// <summary>Checks existing builds do not gain warnings unless they enable the rule.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task Analyze_DiagnosticIsDisabledByDefault()
    {
        var diagnostics = await AnalyzerFixture.RunForBody("[Get(\"/items\")] Task<string> Get([Query(\"client_id\")] string clientId);");
        await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == Rule)).IsFalse();
    }
}
