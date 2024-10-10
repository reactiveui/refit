namespace Refit.Generator;

internal sealed record ContextGenerationModel(
    string RefitInternalNamespace,
    string PreserveAttributeDisplayName,
    ImmutableEquatableArray<InterfaceModel> Interfaces
);
