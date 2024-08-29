namespace Refit.Generator;

internal sealed record ParameterModel(
    string MetadataName,
    string Type,
    bool Annotation,
    bool IsGeneric
);
