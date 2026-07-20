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

    /// <summary>Verifies a nullable value-type (struct) query object flattens inline: the underlying struct's properties
    /// are reached through <c>.Value</c> inside the parameter's <c>HasValue</c> guard, rather than falling back to the
    /// reflection request builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NullableStructQueryObjectFlattensInline()
    {
        const string source =
            """
            #nullable enable
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public struct GeoQuery
            {
                public string? Name { get; set; }
                [Query(Format = "0.00")] public double Lat { get; set; }
            }

            public interface IGeneratedClient
            {
                [Get("/geo")]
                Task<string> Find([Query] GeoQuery? query);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(result.GeneratedSources[Hint]).DoesNotContain(ReflectiveFallback);
    }

    /// <summary>Verifies a query object with nullable and non-nullable dictionary properties (with nullable values)
    /// expands each entry inline under the property key.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task QueryObjectDictionaryPropertiesFlattenInline()
    {
        const string source =
            """
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class DictionaryQuery
            {
                public Dictionary<string, string>? Meta { get; set; }
                public Dictionary<string, string> Tags { get; set; } = new();
            }

            public interface IGeneratedClient
            {
                [Get("/d")]
                Task<string> Find([Query] DictionaryQuery query);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(result.GeneratedSources[Hint]).DoesNotContain(ReflectiveFallback);
    }

    /// <summary>Verifies the default-formatting-local decision recurses into a nested object that needs it, and skips a
    /// nested object plus a scalar that both render only through the formatter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task QueryObjectFormattingLocalDecisionCoversNestedAndFormatterOnly()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public enum Duplicated { First = 1, Alias = 1 }

            public sealed class FormatterOnlyInner { public Duplicated Inner { get; set; } }

            public sealed class FormatterOnlyQuery
            {
                public FormatterOnlyInner Nested { get; set; } = new();
                public Duplicated Extra { get; set; }
            }

            public sealed class FormattedInner { public string Name { get; set; } = ""; }

            public sealed class FormattedQuery
            {
                public FormattedInner Nested { get; set; } = new();
            }

            public interface IGeneratedClient
            {
                [Get("/a")]
                Task<string> FindFormatterOnly([Query] FormatterOnlyQuery query);

                [Get("/b")]
                Task<string> FindFormatted([Query] FormattedQuery query);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(result.GeneratedSources[Hint]).DoesNotContain(ReflectiveFallback);
    }

    /// <summary>Verifies query-object collection properties across every guard shape: a nullable reference collection of
    /// duplicate-constant enums (formatter fallback), a serialize-null collection, and a non-nullable value-type
    /// collection.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task QueryObjectCollectionPropertiesCoverEveryGuardShape()
    {
        const string source =
            """
            using System;
            using System.Collections.Immutable;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public enum Duplicated { First = 1, Alias = 1 }

            public sealed class EnumCollectionQuery
            {
                public Duplicated[] Modes { get; set; } = Array.Empty<Duplicated>();

                [Query(SerializeNull = true)]
                public int[] Optional { get; set; } = Array.Empty<int>();

                public ImmutableArray<int> Fixed { get; set; }
            }

            public interface IGeneratedClient
            {
                [Get("/e")]
                Task<string> Find([Query] EnumCollectionQuery query);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(result.GeneratedSources[Hint]).DoesNotContain(ReflectiveFallback);
    }

    /// <summary>Verifies a value-type dictionary as both a query parameter and a query-object property flattens through
    /// the unguarded (never-null) branch.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValueTypeDictionaryFlattensThroughUnguardedBranch()
    {
        const string source =
            """
            using System.Collections;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public struct StringMap : IDictionary<string, string>
            {
                public string this[string key] { get => throw null!; set => throw null!; }
                public ICollection<string> Keys => throw null!;
                public ICollection<string> Values => throw null!;
                public int Count => throw null!;
                public bool IsReadOnly => throw null!;
                public void Add(string key, string value) => throw null!;
                public void Add(KeyValuePair<string, string> item) => throw null!;
                public void Clear() => throw null!;
                public bool Contains(KeyValuePair<string, string> item) => throw null!;
                public bool ContainsKey(string key) => throw null!;
                public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw null!;
                public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => throw null!;
                public bool Remove(string key) => throw null!;
                public bool Remove(KeyValuePair<string, string> item) => throw null!;
                public bool TryGetValue(string key, out string value) => throw null!;
                IEnumerator IEnumerable.GetEnumerator() => throw null!;
            }

            public sealed class MapQuery
            {
                public StringMap Meta { get; set; }
            }

            public interface IGeneratedClient
            {
                [Get("/m")]
                Task<string> FindMap([Query] StringMap map);

                [Get("/o")]
                Task<string> FindObject([Query] MapQuery query);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(result.GeneratedSources[Hint]).DoesNotContain(ReflectiveFallback);
    }

    /// <summary>Verifies a <c>[Query(Format)]</c> on a non-simple property renders the whole value as a single pair
    /// inline, matching the reflection builder's <c>FormUrlEncodedParameterFormatter.Format(value, format)</c> pass
    /// (which skips flattening), instead of falling back.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task QueryObjectFormatOnNonSimplePropertyRendersWholeValueInline()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Inner { public string Value { get; set; } = ""; }

            public sealed class BadFormatQuery
            {
                [Query(Format = "x")]
                public Inner Nested { get; set; } = new();
            }

            public interface IGeneratedClient
            {
                [Get("/b")]
                Task<string> Find([Query] BadFormatQuery query);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[Hint];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveFallback);

        // The whole value is formatted through the form formatter with the property's format, not flattened.
        await Assert.That(generated).Contains("FormUrlEncodedParameterFormatter.Format(");
        await Assert.That(generated).Contains(", \"x\")");
    }
}
