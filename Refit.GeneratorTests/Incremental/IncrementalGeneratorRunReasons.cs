using Microsoft.CodeAnalysis;

namespace Refit.GeneratorTests.Incremental;

internal record IncrementalGeneratorRunReasons(
    IncrementalStepRunReason BuildRefitStep,
    IncrementalStepRunReason ReportDiagnosticsStep
)
{
    public static readonly IncrementalGeneratorRunReasons New =
        new(IncrementalStepRunReason.New, IncrementalStepRunReason.New);

    public static readonly IncrementalGeneratorRunReasons Cached =
        new(
            // compilation step should always be modified as each time a new compilation is passed
            IncrementalStepRunReason.Cached,
            IncrementalStepRunReason.Unchanged
        );

    public static readonly IncrementalGeneratorRunReasons Modified = Cached with
    {
        ReportDiagnosticsStep = IncrementalStepRunReason.Modified,
        BuildRefitStep = IncrementalStepRunReason.Modified,
    };

    public static readonly IncrementalGeneratorRunReasons ModifiedSource = Cached with
    {
        ReportDiagnosticsStep = IncrementalStepRunReason.Unchanged,
        BuildRefitStep = IncrementalStepRunReason.Modified,
    };
}
