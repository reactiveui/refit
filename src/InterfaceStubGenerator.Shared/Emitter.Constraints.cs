// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Emits generic type-parameter constraint clauses for generated Refit method implementations.</summary>
internal static partial class Emitter
{
    /// <summary>
    /// The number of keyword constraints (<c>class</c>, <c>unmanaged</c>, <c>struct</c>, <c>notnull</c>, <c>new()</c>)
    /// that can be emitted alongside a type parameter's declared type constraints.
    /// </summary>
    private const int KeywordConstraintCount = 5;

    /// <summary>Builds the generic type constraint clauses for the given type parameters.</summary>
    /// <param name="typeParameters">The type parameter constraints to emit.</param>
    /// <param name="isOverrideOrExplicitImplementation">True if emitting for an override or explicit implementation.</param>
    /// <param name="indentationLevel">The generated indentation level.</param>
    /// <returns>The generated type constraint clauses.</returns>
    internal static string BuildConstraints(
        ImmutableEquatableArray<TypeConstraint> typeParameters,
        bool isOverrideOrExplicitImplementation,
        int indentationLevel)
    {
        // The overwhelmingly common case is a non-generic method: skip the array allocation entirely.
        if (typeParameters.Count == 0)
        {
            return string.Empty;
        }

        var parts = new string[typeParameters.Count];
        var count = 0;
        for (var i = 0; i < typeParameters.Count; i++)
        {
            var source = BuildConstraintsForTypeParameter(
                typeParameters[i],
                isOverrideOrExplicitImplementation,
                indentationLevel);
            if (source.Length != 0)
            {
                parts[count] = source;
                count++;
            }
        }

        return count == 0 ? string.Empty : ConcatParts(parts, count);
    }

    /// <summary>Builds the constraint clause for a single type parameter.</summary>
    /// <param name="typeParameter">The type parameter constraint to emit.</param>
    /// <param name="isOverrideOrExplicitImplementation">True if emitting for an override or explicit implementation.</param>
    /// <param name="indentationLevel">The generated indentation level.</param>
    /// <returns>The generated type constraint clause, or an empty string.</returns>
    internal static string BuildConstraintsForTypeParameter(
        in TypeConstraint typeParameter,
        bool isOverrideOrExplicitImplementation,
        int indentationLevel) =>
        !HasConstraintKeywords(typeParameter, isOverrideOrExplicitImplementation)
            ? string.Empty
            : Indent(indentationLevel)
                + "where "
                + typeParameter.TypeName
                + " : "
                + BuildConstraintList(typeParameter, isOverrideOrExplicitImplementation)
                + "\n";

    /// <summary>Determines whether a type parameter has constraints that should be emitted.</summary>
    /// <param name="typeParameter">The type parameter constraint to inspect.</param>
    /// <param name="isOverrideOrExplicitImplementation">True if emitting for an override or explicit implementation.</param>
    /// <returns><see langword="true"/> when at least one constraint should be emitted.</returns>
    internal static bool HasConstraintKeywords(
        in TypeConstraint typeParameter,
        bool isOverrideOrExplicitImplementation)
    {
        var knownConstraints = typeParameter.KnownTypeConstraint;
        return (knownConstraints & KnownTypeConstraint.Class) != 0
               || ((knownConstraints & KnownTypeConstraint.Unmanaged) != 0 && !isOverrideOrExplicitImplementation)
               || (knownConstraints & KnownTypeConstraint.Struct) != 0
               || ((knownConstraints & KnownTypeConstraint.NotNull) != 0 && !isOverrideOrExplicitImplementation)
               || (!isOverrideOrExplicitImplementation && (typeParameter.Constraints.Count > 0 ||
                                                           (knownConstraints & KnownTypeConstraint.New) != 0));
    }

    /// <summary>Builds the comma-separated constraint list for a type parameter.</summary>
    /// <param name="typeParameter">The type parameter constraint to inspect.</param>
    /// <param name="isOverrideOrExplicitImplementation">True if emitting for an override or explicit implementation.</param>
    /// <returns>The generated constraint list.</returns>
    internal static string BuildConstraintList(
        in TypeConstraint typeParameter,
        bool isOverrideOrExplicitImplementation)
    {
        var parts = new string[typeParameter.Constraints.Count + KeywordConstraintCount];
        var count = 0;
        var knownConstraints = typeParameter.KnownTypeConstraint;
        AddConstraint(parts, "class", (knownConstraints & KnownTypeConstraint.Class) != 0, ref count);
        AddConstraint(
            parts,
            "unmanaged",
            (knownConstraints & KnownTypeConstraint.Unmanaged) != 0 && !isOverrideOrExplicitImplementation,
            ref count);
        AddConstraint(parts, "struct", (knownConstraints & KnownTypeConstraint.Struct) != 0, ref count);
        AddConstraint(
            parts,
            "notnull",
            (knownConstraints & KnownTypeConstraint.NotNull) != 0 && !isOverrideOrExplicitImplementation,
            ref count);

        if (!isOverrideOrExplicitImplementation)
        {
            foreach (var constraint in typeParameter.Constraints)
            {
                AddConstraint(parts, constraint, true, ref count);
            }
        }

        AddConstraint(
            parts,
            "new()",
            (knownConstraints & KnownTypeConstraint.New) != 0 && !isOverrideOrExplicitImplementation,
            ref count);
        return JoinParts(parts, count, ", ");
    }

    /// <summary>Adds one constraint keyword when the condition is true.</summary>
    /// <param name="parts">The target constraint buffer.</param>
    /// <param name="keyword">The constraint keyword.</param>
    /// <param name="condition">Whether the keyword should be emitted.</param>
    /// <param name="count">The populated part count.</param>
    internal static void AddConstraint(
        string[] parts,
        string keyword,
        bool condition,
        ref int count)
    {
        if (!condition)
        {
            return;
        }

        parts[count] = keyword;
        count++;
    }
}
