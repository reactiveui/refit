namespace Refit.Generator;

internal sealed record InterfaceModel(
    string PreserveAttributeDisplayName,
    string FileName,
    string ClassName,
    string Ns,
    string ClassDeclaration,
    string InterfaceDisplayName,
    string ClassSuffix,
    ImmutableEquatableArray<TypeConstraint> Constraints,
    ImmutableEquatableArray<string> MemberNames,
    ImmutableEquatableArray<MethodModel> NonRefitMethods,
    ImmutableEquatableArray<MethodModel> RefitMethods,
    ImmutableEquatableArray<MethodModel> DerivedRefitMethods,
    Nullability Nullability,
    bool DisposeMethod
);

internal enum Nullability : byte
{
    Enabled,
    Disabled,
    None
}
