// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Refit.LiveQuery;

/// <summary>Contains the models used by the generated query API.</summary>
public static partial class LiveQueryApi
{
    /// <summary>Represents a nullable value-type query object.</summary>
    public readonly record struct GeoPoint
    {
        /// <summary>Gets the point name.</summary>
        public string? Name { get; init; }

        /// <summary>Gets the latitude.</summary>
        [Query(Format = "0.00")]
        public double Lat { get; init; }
    }

    /// <summary>Represents a request body used with the custom QUERY verb.</summary>
    public sealed class CreatePayload
    {
        /// <summary>Gets or sets the payload name.</summary>
        public string? Name { get; set; }
    }

    /// <summary>Contains properties substituted into a dotted route.</summary>
    public sealed class RouteInfo
    {
        /// <summary>Gets or sets the route slug.</summary>
        public string? Slug { get; set; }

        /// <summary>Gets or sets the route version.</summary>
        public int Version { get; set; }
    }

    /// <summary>Represents a nested customer route value.</summary>
    public sealed class NestedCustomer
    {
        /// <summary>Gets or sets the customer identifier.</summary>
        public string? Id { get; set; }
    }

    /// <summary>Represents an order with nested route data.</summary>
    public sealed class NestedOrder
    {
        /// <summary>Gets or sets the customer.</summary>
        public NestedCustomer? Customer { get; set; }

        /// <summary>Gets or sets an unrelated residual query value.</summary>
        public string? Note { get; set; }
    }

    /// <summary>Provides a custom string representation for a route value.</summary>
    public sealed class RouteToken
    {
        /// <summary>Gets or sets the route value.</summary>
        public string? Value { get; set; }

        /// <inheritdoc/>
        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>Represents formatted range bounds.</summary>
    public sealed class Bounds
    {
        /// <summary>Gets or sets the minimum.</summary>
        public int Min { get; set; }

        /// <summary>Gets or sets the maximum.</summary>
        public int Max { get; set; }

        /// <inheritdoc/>
        public override string ToString() => $"{Min}..{Max}";
    }

    /// <summary>Wraps formatted range bounds as a query object.</summary>
    public sealed class RangeQuery
    {
        /// <summary>Gets or sets the query window.</summary>
        [Query(Format = "g")]
        public Bounds? Window { get; set; }
    }

    /// <summary>Represents a non-sealed dictionary query value.</summary>
    public class Facet
    {
        /// <summary>Gets or sets the facet name.</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the facet count.</summary>
        public int Count { get; set; }
    }

    /// <summary>Provides a derived facet so the base fixture remains intentionally unsealed.</summary>
    public sealed class DerivedFacet : Facet;

    /// <summary>Defines the custom HTTP QUERY verb.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class QueryVerbAttribute : HttpMethodAttribute
    {
        /// <summary>Initializes a new instance of the <see cref="QueryVerbAttribute"/> class.</summary>
        /// <param name="path">The request path.</param>
        public QueryVerbAttribute(string path)
            : base(path)
        {
        }

        /// <inheritdoc/>
        public override HttpMethod Method => new("QUERY");
    }

    /// <summary>Represents one indexed query item.</summary>
    public sealed class Item
    {
        /// <summary>Gets or sets the item identifier.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the item value.</summary>
        public string? Value { get; set; }
    }

    /// <summary>Represents a name with a serialized property alias.</summary>
    public sealed class Name
    {
        /// <summary>Gets or sets the first name.</summary>
        [JsonPropertyName("First Name")]
        public string? FirstName { get; set; }

        /// <summary>Gets or sets the last name.</summary>
        public string? LastName { get; set; }
    }
}
