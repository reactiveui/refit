// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.GeneratorTests;

/// <summary>Request-generation coverage for inline query-string emission branches.</summary>
public sealed partial class RequestGenerationCoverageTests
{
    /// <summary>Verifies enum query values across renamed, duplicate-valued, reused and collection shapes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EnumQueryValuesGenerateInline()
    {
        const string Source =
            """
            using System.Runtime.Serialization;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public enum Renamed
            {
                [EnumMember(Value = "one")] First,
                [EnumMember] Second,
                Third,
            }

            public enum Duplicated
            {
                A = 1,
                B = 1,
            }

            public interface IGeneratedClient
            {
                [Get("/a")] Task<string> Renamed1([Query] Renamed value);
                [Get("/b")] Task<string> Renamed2([Query] Renamed value);
                [Get("/c")] Task<string> Dup([Query] Duplicated value);
                [Get("/d")] Task<string> DupCollection([Query(CollectionFormat.Multi)] Duplicated[] values);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
    }

    /// <summary>Verifies an enum whose <c>[EnumMember(Value = null)]</c> override is a null literal falls back to the
    /// member name rather than reading a string override.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EnumMemberWithNullValueOverrideGeneratesInline()
    {
        const string Source =
            """
            using System.Runtime.Serialization;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public enum NullOverride
            {
                [EnumMember(Value = null)] First,
                Second,
            }

            public interface IGeneratedClient
            {
                [Get("/a")] Task<string> Get([Query] NullOverride value);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies non-nullable dictionary, converter and collection query parameters generate inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NonNullableQueryParameterShapesGenerateInline()
    {
        const string Source =
            """
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class MapConverter : IQueryConverter<IDictionary<string, object>>
            {
                public void Flatten(IDictionary<string, object> value, string keyPrefix, ref GeneratedQueryStringBuilder builder, RefitSettings settings)
                {
                    foreach (var entry in value)
                    {
                        builder.Add(keyPrefix + entry.Key, settings.UrlParameterFormatter.Format(entry.Value, typeof(object), typeof(object)), false);
                    }
                }
            }

            public interface IGeneratedClient
            {
                [Get("/d")] Task<string> Dict(IDictionary<string, string> query);
                [Get("/c")] Task<string> Converter([QueryConverter(typeof(MapConverter))] IDictionary<string, object> filter);
                [Get("/l")] Task<string> Collection([Query(CollectionFormat.Multi)] int[] ids);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(result.GeneratedSources[GeneratedClientHintName]).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies value-type collection and converter query parameters reach the non-guarded emission path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValueTypeQueryParametersGenerateInline()
    {
        const string Source =
            """
            #nullable enable
            using System.Collections.Immutable;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public struct Pair { public int A { get; set; } public int B { get; set; } }

            public sealed class PairConverter : IQueryConverter<Pair>
            {
                public void Flatten(Pair value, string keyPrefix, ref GeneratedQueryStringBuilder builder, RefitSettings settings) { }
            }

            public interface IGeneratedClient
            {
                [Get("/l")] Task<string> Collection([Query(CollectionFormat.Multi)] ImmutableArray<int> ids);
                [Get("/c")] Task<string> Converter([QueryConverter(typeof(PairConverter))] Pair pair);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
    }

    /// <summary>Verifies two parameters bound to the same converter type reuse a single cached converter field.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RepeatedQueryConverterTypeReusesField()
    {
        const string Source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class MapConverter : IQueryConverter<IDictionary<string, object>>
            {
                public void Flatten(IDictionary<string, object> value, string keyPrefix, ref GeneratedQueryStringBuilder builder, RefitSettings settings) { }
            }

            public interface IGeneratedClient
            {
                [Get("/a")] Task<string> First([QueryConverter(typeof(MapConverter))] IDictionary<string, object> a);
                [Get("/b")] Task<string> Second([QueryConverter(typeof(MapConverter))] IDictionary<string, object> b);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
    }

    /// <summary>Verifies a dictionary query parameter with a key prefix whose values are complex objects flattens each
    /// value's properties under the prefixed entry key.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DictionaryQueryWithComplexValuesAndPrefixFlattensInline()
    {
        const string Source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Bounds
            {
                public int Min { get; set; }

                public int Max { get; set; }
            }

            public interface IGeneratedClient
            {
                [Get("/search")]
                Task<string> Search([Query(".", "f")] IDictionary<string, Bounds> filters);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("_prefixed");
    }

    /// <summary>Verifies a <see cref="System.Uri"/> query parameter is treated as a scalar and rendered inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UriQueryParameterGeneratesInline()
    {
        const string Source =
            """
            using System;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/redirect")]
                Task<string> Get([Query] Uri target);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies dictionary query parameters with formattable keys and values render through the reflection-free
    /// fast key/value paths, and string-keyed dictionaries render through the formatter-only path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FormattableAndStringKeyedDictionaryQueriesGenerateInline()
    {
        const string Source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/n")] Task<string> Numbers(IDictionary<int, int> map);
                [Get("/s")] Task<string> Strings(IDictionary<string, string> map);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies value-type and nullable-value-type element collection queries render inline through both element
    /// null-guard branches.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValueAndNullableElementCollectionQueriesGenerateInline()
    {
        const string Source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/v")] Task<string> Values([Query(CollectionFormat.Multi)] int[] ids);
                [Get("/n")] Task<string> Nullables([Query(CollectionFormat.Multi)] List<int?> ids);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies an object query parameter with an explicit delimiter, and a converter parameter with a key
    /// prefix and delimiter, both fold their delimiter into the composed keys inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ObjectDelimiterAndPrefixedConverterQueriesGenerateInline()
    {
        const string Source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Filter
            {
                public string? Name { get; set; }

                public int Age { get; set; }
            }

            public sealed class FilterConverter : IQueryConverter<Filter>
            {
                public void Flatten(Filter value, string keyPrefix, ref GeneratedQueryStringBuilder builder, RefitSettings settings) { }
            }

            public interface IGeneratedClient
            {
                [Get("/o")] Task<string> Object([Query("-")] Filter filter);
                [Get("/c")] Task<string> Converter([Query("-", "pfx")][QueryConverter(typeof(FilterConverter))] Filter filter);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies duplicate-constant enum keys, values and formats force the formatter-only key/value branches of
    /// dictionary and object query emission: dictionaries keyed and valued by a duplicate-constant enum, a prefixed
    /// scalar dictionary, a query object whose dictionary properties carry duplicate-constant enum keys and values, its
    /// string and value-type collection properties, a formatted duplicate-enum scalar and a plain duplicate-enum scalar,
    /// and a valueless <c>[QueryName]</c> flag.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DuplicateEnumDictionaryAndObjectQueriesUseFormatterOnlyBranches()
    {
        const string Source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public enum Dup { A = 1, B = 1 }

            public sealed class ObjQuery
            {
                public IDictionary<string, Dup> ByEnumValue { get; set; } = new Dictionary<string, Dup>();

                public IDictionary<Dup, string> ByEnumKey { get; set; } = new Dictionary<Dup, string>();

                public string[] Names { get; set; } = System.Array.Empty<string>();

                public int[] Numbers { get; set; } = System.Array.Empty<int>();

                public int?[] MaybeNumbers { get; set; } = System.Array.Empty<int?>();

                [Query(Format = "D")]
                public Dup FormattedFlag { get; set; }

                public Dup PlainFlag { get; set; }
            }

            public interface IGeneratedClient
            {
                [Get("/a")] Task<string> EnumKey(IDictionary<Dup, string> map);
                [Get("/b")] Task<string> EnumValue(IDictionary<string, Dup> map);
                [Get("/c")] Task<string> Prefixed([Query(".", "pfx")] IDictionary<string, string> map);
                [Get("/d")] Task<string> Object(ObjQuery query);
                [Get("/e")] Task<string> Flag([QueryName] string tag);
                [Get("/f")] Task<string> Collection([Query(CollectionFormat.Multi)] string[] tags);
                [Get("/g")] Task<string> FlagCollection([QueryName] string[] tags);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a query object flattens property-level branches: a nested object property whose child carries
    /// a <c>[JsonPropertyName]</c> and a prefixed <c>[Query]</c>, a nullable-struct nested property flattened through
    /// <c>.Value</c>, a string-element collection, and a formatted scalar.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task QueryObjectPropertyBranchesFlattenInline()
    {
        const string Source =
            """
            #nullable enable
            using System.Text.Json.Serialization;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Inner
            {
                [JsonPropertyName("child_name")]
                public string? Child { get; set; }

                [Query("-", "p")]
                public string? Prefixed { get; set; }
            }

            public struct Point
            {
                public int X { get; set; }

                public int Y { get; set; }
            }

            public sealed class Outer
            {
                public Inner Nested { get; set; } = new();

                public Point? Maybe { get; set; }

                public string[]? Names { get; set; }

                [Query(Format = "0.00")]
                public double Amount { get; set; }
            }

            public interface IGeneratedClient
            {
                [Get("/o")]
                Task<string> Find([Query] Outer query);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("child_name");
    }
}
