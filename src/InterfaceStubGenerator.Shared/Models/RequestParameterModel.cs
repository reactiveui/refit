// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Generator;

/// <summary>Describes how a method parameter contributes to a request.</summary>
/// <param name="Identity">The parameter name, type and nullability.</param>
/// <param name="Path">The route placeholder bindings.</param>
/// <param name="Attributes">The attributes available to formatters.</param>
/// <param name="Kind">The primary request binding kind.</param>
/// <param name="Header">The dynamic header binding.</param>
/// <param name="PropertyKey">The request option key.</param>
/// <param name="Body">The body serialization and compression strategy.</param>
/// <param name="AllowRelativeUrl">Whether a URL parameter accepts relative URI references.</param>
internal readonly record struct RequestParameterModel(
    RequestParameterIdentityModel Identity,
    RequestParameterPathModel Path,
    ParameterAttributeModel[] Attributes,
    RequestParameterKind Kind,
    RequestParameterHeaderModel Header,
    string PropertyKey,
    RequestParameterBodyModel Body,
    bool AllowRelativeUrl = false)
{
    /// <summary>
    /// Gets the query-binding metadata when this parameter feeds the query string — set for
    /// <see cref="RequestParameterKind.Query"/> parameters and for <see cref="RequestParameterKind.Property"/>
    /// parameters that also carry <c>[Query]</c>.
    /// </summary>
    internal QueryParameterModel? Query { get; init; }

    /// <summary>Gets the fully-qualified type argument of a <see cref="RequestParameterKind.JsonTypeInfo"/> parameter,
    /// the type its metadata describes, or <see langword="null"/> for every other parameter.</summary>
    internal string? JsonTypeInfoTarget { get; init; }

    /// <summary>Compares the parsed metadata, including array contents.</summary>
    /// <param name="other">The metadata to compare.</param>
    /// <returns>Whether the metadata is equal.</returns>
    public bool Equals(RequestParameterModel other) =>
        EqualityComparer<RequestParameterIdentityModel>.Default.Equals(
            Identity,
            other.Identity)
        && EqualityComparer<RequestParameterPathModel>.Default.Equals(
            Path,
            other.Path)
        && GeneratorModelEquality.SequenceEquals(
            Attributes,
            other.Attributes)
        && EqualityComparer<RequestParameterKind>.Default.Equals(
            Kind,
            other.Kind)
        && EqualityComparer<RequestParameterHeaderModel>.Default.Equals(
            Header,
            other.Header)
        && EqualityComparer<string>.Default.Equals(
            PropertyKey,
            other.PropertyKey)
        && EqualityComparer<RequestParameterBodyModel>.Default.Equals(
            Body,
            other.Body)
        && EqualityComparer<bool>.Default.Equals(
            AllowRelativeUrl,
            other.AllowRelativeUrl)
        && EqualityComparer<QueryParameterModel?>.Default.Equals(
            Query,
            other.Query)
        && EqualityComparer<string?>.Default.Equals(JsonTypeInfoTarget, other.JsonTypeInfoTarget);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Identity));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Path));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetSequenceHashCode(Attributes));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Kind));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Header));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(PropertyKey));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Body));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(AllowRelativeUrl));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(Query));
        hash = GeneratorModelEquality.Combine(hash, GeneratorModelEquality.GetHashCode(JsonTypeInfoTarget));
        return hash;
    }
}
