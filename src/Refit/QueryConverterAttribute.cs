// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>
/// Flattens a query parameter with a hand-written <see cref="IQueryConverter{T}"/> instead of the source generator's
/// declared-type walk, for shapes that are only known at runtime (an <see cref="object"/> value, a polymorphic base
/// type, a <c>Dictionary&lt;string, object&gt;</c>, and similar).
/// </summary>
/// <remarks>
/// This is a source-generation-only attribute: it lets an otherwise-unflattenable parameter generate inline. A method
/// that carries it but cannot generate inline for another reason reports <c>RF007</c>. The named converter type must
/// implement <see cref="IQueryConverter{T}"/> for the parameter's declared type and expose a public parameterless
/// constructor.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class QueryConverterAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="QueryConverterAttribute"/> class.</summary>
    /// <param name="converterType">The <see cref="IQueryConverter{T}"/> implementation to flatten the parameter with.</param>
    public QueryConverterAttribute(Type converterType) => ConverterType = converterType;

    /// <summary>Gets the <see cref="IQueryConverter{T}"/> implementation type.</summary>
    public Type ConverterType { get; }
}
