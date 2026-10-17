// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>
/// A precomputed attribute to re-emit onto a generated parameter's attribute provider. The parser flattens the
/// Roslyn <c>AttributeData</c> into value-typed expressions so no symbols are held in the incremental cache.
/// </summary>
/// <param name="TypeExpression">The fully-qualified attribute type expression, e.g. <c>global::Refit.AliasAsAttribute</c>.</param>
/// <param name="ConstructorArguments">The attribute's constructor argument expressions, in order.</param>
/// <param name="NamedArguments">The attribute's named argument assignments.</param>
internal readonly record struct ParameterAttributeModel(
    string TypeExpression,
    string[] ConstructorArguments,
    NamedAttributeArgument[] NamedArguments)
{
    /// <summary>Compares all parsed values, including array contents.</summary>
    /// <param name = "other">The model to compare.</param>
    /// <returns>Whether the models describe the same input.</returns>
    public bool Equals(ParameterAttributeModel other) =>
        EqualityComparer<string>.Default.Equals(
            TypeExpression,
            other.TypeExpression)
        && GeneratorModelEquality.SequenceEquals(
            ConstructorArguments,
            other.ConstructorArguments)
        && GeneratorModelEquality.SequenceEquals(NamedArguments, other.NamedArguments);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(TypeExpression));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(ConstructorArguments));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(NamedArguments));
        return hash;
    }
}
