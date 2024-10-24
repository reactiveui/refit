namespace Refit.Generator;

internal sealed record MethodModel(
    string Name,
    string ReturnType,
    string ContainingType,
    string DeclaredMethod,
    ReturnTypeInfo ReturnTypeMetadata,
    ImmutableEquatableArray<ParameterModel> Parameters,
    ImmutableEquatableArray<TypeConstraint> Constraints
);

internal enum ReturnTypeInfo : byte
{
    Return,
    AsyncVoid,
    AsyncResult
}
