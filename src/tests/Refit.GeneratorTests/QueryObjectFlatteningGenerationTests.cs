// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.GeneratorTests;

/// <summary>
/// Verifies that generated request building flattens <c>[Query]</c> object parameters inline across the property
/// shapes the reflection builder supports: nested objects, collection properties, per-property formats and names,
/// serialize-null scalars, ignored properties, and enum properties, for both nullable and non-nullable parameters.
/// </summary>
public sealed class QueryObjectFlatteningGenerationTests
{
    /// <summary>The generated implementation source hint name.</summary>
    private const string Hint = "IGeneratedClient.g.cs";

    /// <summary>The reflective request-builder call emitted by fallback paths.</summary>
    private const string ReflectiveFallback = "BuildRestResultFuncForMethod";

    /// <summary>The Refit source declaring a query object exercising every inline-supported property shape.</summary>
    private const string QueryObjectSource =
        """
        #nullable enable
        using System.Collections.Generic;
        using System.Runtime.Serialization;
        using System.Text.Json.Serialization;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public enum SortKind
        {
            [EnumMember(Value = "date-desc")] DateDescending,
            Name,
        }

        public sealed class Address
        {
            public string? City { get; set; }
            public int Zip { get; set; }
        }

        public sealed class CustomerQuery
        {
            public string? Name { get; set; }
            public Address? Home { get; set; }
            public Address Work { get; set; } = new();
            public int[]? Ids { get; set; }
            [Query(CollectionFormat.Multi)] public IReadOnlyList<SortKind>? Tags { get; set; }
            [Query(Format = "0.00")] public double Amount { get; set; }
            [Query(SerializeNull = true)] public string? Note { get; set; }
            [JsonPropertyName("json_name")] public string? Named { get; set; }
            [AliasAs("alias")] public string? Aliased { get; set; }
            [JsonIgnore] public string? Skipped { get; set; }
            public SortKind Sort { get; set; }
        }

        public interface IGeneratedClient
        {
            [Get("/customers")]
            Task<string> FindNullable([Query] CustomerQuery? query);

            [Get("/customers")]
            Task<string> FindNonNullable([Query] CustomerQuery query);
        }
        """;

    /// <summary>
    /// A value-type (struct) query object. Struct parameters and struct nested properties are never null, so they
    /// exercise the non-guarded flattening branches that a reference type (always treated as nullable) never reaches.
    /// </summary>
    private const string StructQueryObjectSource =
        """
        #nullable enable
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public sealed class Waypoint
        {
            public string? City { get; set; }
            [AliasAs("z")] public string? Zip { get; set; }
        }

        public struct Coord
        {
            [Query(Format = "0.00")] public double Lat { get; set; }
            public double Lng { get; set; }
        }

        public struct GeoQuery
        {
            public string? Name { get; set; }
            public Coord Origin { get; set; }
            public Coord? Peak { get; set; }
            [Query(SerializeNull = true)] public Coord? Base { get; set; }
            [Query(SerializeNull = true)] public Waypoint? Fallback { get; set; }
            [AliasAs("wp")] public Waypoint? Marker { get; set; }
            [Query(Format = "0.0")] public double Amount { get; set; }
            [Query(CollectionFormat.Multi)] public int[]? Tags { get; set; }
        }

        public interface IGeneratedClient
        {
            [Get("/geo")]
            Task<string> Find([Query] GeoQuery query);
        }
        """;

    /// <summary>Verifies the query object flattens inline for both nullable and non-nullable parameters.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task QueryObjectFlattensInline()
    {
        var result = Fixture.RunGenerator(QueryObjectSource, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(result.GeneratedSources[Hint]).DoesNotContain(ReflectiveFallback);
    }

    /// <summary>Verifies a struct query object flattens inline through its non-null nested and collection properties.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StructQueryObjectFlattensInline()
    {
        var result = Fixture.RunGenerator(StructQueryObjectSource, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(result.GeneratedSources[Hint]).DoesNotContain(ReflectiveFallback);
    }
}
