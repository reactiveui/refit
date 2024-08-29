namespace Refit.Generator;

internal readonly record struct TypeConstraint(
    string TypeName,
    string DeclaredName,
    KnownTypeConstraint KnownTypeConstraint,
    ImmutableEquatableArray<string> Constraints
);

[Flags]
internal enum KnownTypeConstraint : byte
{
    None = 0,
    Class = 1 << 0,
    Unmanaged = 1 << 1,
    Struct = 1 << 2,
    NotNull = 1 << 3,
    New = 1 << 4
}
