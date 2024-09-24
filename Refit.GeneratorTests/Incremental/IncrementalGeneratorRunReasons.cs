using Microsoft.CodeAnalysis;

namespace Refit.GeneratorTests.Incremental;

internal record IncrementalGeneratorRunReasons(
    IncrementalStepRunReason BuildMediatorStep,
    IncrementalStepRunReason ReportDiagnosticsStep
)
{
    public static readonly IncrementalGeneratorRunReasons New =
        new(IncrementalStepRunReason.New, IncrementalStepRunReason.New);

    public static readonly IncrementalGeneratorRunReasons Cached =
        new(
            // compilation step should always be modified as each time a new compilation is passed
            IncrementalStepRunReason.Cached,
            IncrementalStepRunReason.Cached
        );

    public static readonly IncrementalGeneratorRunReasons Modified = Cached with
    {
        ReportDiagnosticsStep = IncrementalStepRunReason.Modified,
        BuildMediatorStep = IncrementalStepRunReason.Modified,
    };

    public static readonly IncrementalGeneratorRunReasons ModifiedSource = Cached with
    {
        ReportDiagnosticsStep = IncrementalStepRunReason.Unchanged,
        BuildMediatorStep = IncrementalStepRunReason.Modified,
    };
}
