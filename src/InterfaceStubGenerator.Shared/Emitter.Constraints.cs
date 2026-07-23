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

    /// <summary>Appends the generic type constraint clauses for the given type parameters.</summary>
    /// <param name="builder">The buffer accumulating the interface source.</param>
    /// <param name="typeParameters">The type parameter constraints to emit.</param>
    /// <param name="isOverrideOrExplicitImplementation">True if emitting for an override or explicit implementation.</param>
    /// <param name="indentationLevel">The generated indentation level.</param>
    internal static void AppendConstraints(
        PooledStringBuilder builder,
        ImmutableEquatableArray<TypeConstraint> typeParameters,
        bool isOverrideOrExplicitImplementation,
        int indentationLevel)
    {
        // The overwhelmingly common case is a non-generic method: skip the array allocation entirely.
        if (typeParameters.Count == 0)
        {
            return;
        }

        for (var i = 0; i < typeParameters.Count; i++)
        {
            AppendConstraintsForTypeParameter(
                builder,
                typeParameters[i],
                isOverrideOrExplicitImplementation,
                indentationLevel);
        }
    }

    /// <summary>Appends the constraint clause for a single type parameter.</summary>
    /// <param name="builder">The buffer accumulating the interface source.</param>
    /// <param name="typeParameter">The type parameter constraint to emit.</param>
    /// <param name="isOverrideOrExplicitImplementation">True if emitting for an override or explicit implementation.</param>
    /// <param name="indentationLevel">The generated indentation level.</param>
    internal static void AppendConstraintsForTypeParameter(
        PooledStringBuilder builder,
        in TypeConstraint typeParameter,
        bool isOverrideOrExplicitImplementation,
        int indentationLevel)
    {
        if (!HasConstraintKeywords(typeParameter, isOverrideOrExplicitImplementation))
        {
            return;
        }

        _ = builder.Append(Indent(indentationLevel)).Append("where ").Append(typeParameter.TypeName).Append(" : ");
        AppendConstraintList(builder, typeParameter, isOverrideOrExplicitImplementation);
        _ = builder.AppendLine();
    }

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

    /// <summary>Appends the comma-separated constraint list for a type parameter.</summary>
    /// <param name="builder">The buffer accumulating the interface source.</param>
    /// <param name="typeParameter">The type parameter constraint to inspect.</param>
    /// <param name="isOverrideOrExplicitImplementation">True if emitting for an override or explicit implementation.</param>
    internal static void AppendConstraintList(
        PooledStringBuilder builder,
        in TypeConstraint typeParameter,
        bool isOverrideOrExplicitImplementation)
    {
        var knownConstraints = typeParameter.KnownTypeConstraint;
        var addComma = false;
        AddConstraint(builder, "class", (knownConstraints & KnownTypeConstraint.Class) != 0, ref addComma);
        AddConstraint(
            builder,
            "unmanaged",
            (knownConstraints & KnownTypeConstraint.Unmanaged) != 0 && !isOverrideOrExplicitImplementation,
            ref addComma);
        AddConstraint(builder, "struct", (knownConstraints & KnownTypeConstraint.Struct) != 0, ref addComma);
        AddConstraint(
            builder,
            "notnull",
            (knownConstraints & KnownTypeConstraint.NotNull) != 0 && !isOverrideOrExplicitImplementation,
            ref addComma);

        if (!isOverrideOrExplicitImplementation)
        {
            foreach (var constraint in typeParameter.Constraints)
            {
                AddConstraint(builder, constraint, true, ref addComma);
            }
        }

        AddConstraint(
            builder,
            "new()",
            (knownConstraints & KnownTypeConstraint.New) != 0 && !isOverrideOrExplicitImplementation,
            ref addComma);
    }

    /// <summary>Adds one constraint keyword when the condition is true.</summary>
    /// <param name="builder">The buffer accumulating the interface source.</param>
    /// <param name="keyword">The constraint keyword.</param>
    /// <param name="condition">Whether the keyword should be emitted.</param>
    /// <param name="addComma">Whether a comma should be prepended.</param>
    internal static void AddConstraint(
        PooledStringBuilder builder,
        string keyword,
        bool condition,
        ref bool addComma)
    {
        if (!condition)
        {
            return;
        }

        if (addComma)
        {
            _ = builder.Append(", ");
        }

        addComma = true;
        _ = builder.Append(keyword);
    }
}
