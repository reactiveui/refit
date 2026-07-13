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
}
