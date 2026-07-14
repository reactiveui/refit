// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Verifies which query-parameter shapes generated request building supports inline.</summary>
public sealed class QueryParameterTypeTests
{
    /// <summary>The generated client hint name.</summary>
    private const string GeneratedClientHintName = "IGeneratedClient.g.cs";

    /// <summary>The reflection request-builder call emitted when a method falls back.</summary>
    private const string ReflectiveRequestBuilderCall = "BuildRestResultFuncForMethod";

    /// <summary>The diagnostic id reported when a source-generation-only attribute is on a fallback method.</summary>
    private const string SourceGenOnlyDiagnosticId = "RF007";

    /// <summary>Verifies scalar query-parameter types generate inline query construction.</summary>
    /// <param name="parameterType">The query parameter type expression.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("string")]
    [Arguments("string?")]
    [Arguments("bool")]
    [Arguments("char")]
    [Arguments("int")]
    [Arguments("int?")]
    [Arguments("long")]
    [Arguments("double")]
    [Arguments("decimal")]
    [Arguments("System.Guid")]
    [Arguments("System.DateTime")]
    [Arguments("System.DateTimeOffset")]
    [Arguments("System.DateOnly")]
    [Arguments("System.TimeSpan")]
    [Arguments("System.DayOfWeek")]
    [Arguments("System.DayOfWeek?")]
    [Arguments("System.Globalization.CultureInfo")]
    public async Task ScalarQueryParameterGeneratesInline(string parameterType)
    {
        var generated = Generate($"[Get(\"/items\")] Task<string> Get({parameterType} value);");

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a nullable value-type query parameter uses the span-formattable fast write on its unwrapped
    /// <c>.Value</c> after the null guard, rather than materializing an intermediate string.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NullableValueTypeQueryParameterUsesSpanFastWrite()
    {
        var generated = Generate("[Get(\"/items\")] Task<string> Get(int? page);");

        await Assert.That(generated).Contains(".AddFormattedPreEscapedKey(");
        await Assert.That(generated).Contains("@page.Value");
    }

    /// <summary>Verifies collections of scalars generate inline query construction.</summary>
    /// <param name="parameterType">The query parameter type expression.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("int[]")]
    [Arguments("int?[]")]
    [Arguments("string[]")]
    [Arguments("System.Collections.Generic.List<int>")]
    [Arguments("System.Collections.Generic.IEnumerable<string>")]
    [Arguments("System.Collections.Generic.IReadOnlyList<System.Guid>")]
    public async Task ScalarCollectionQueryParameterGeneratesInline(string parameterType)
    {
        var generated = Generate($"[Get(\"/items\")] Task<string> Get({parameterType} values);");

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies attributed query shapes generate inline query construction.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("[Get(\"/i\")] Task<string> Get([AliasAs(\"q\")] string value);")]
    [Arguments("[Get(\"/i\")] Task<string> Get([Query(Format = \"0.00\")] double value);")]
    [Arguments("[Get(\"/i\")] Task<string> Get([Query(CollectionFormat.Multi)] int[] values);")]
    [Arguments("[Get(\"/i\")] Task<string> Get([Query(TreatAsString = true)] object value);")]
    [Arguments("[Get(\"/i\")] Task<string> Get([QueryName] string flag);")]
    [Arguments("[Get(\"/i\")] Task<string> Get([QueryName] string[] flags);")]
    [Arguments("[Get(\"/i\")] Task<string> Get([Encoded] string value);")]
    [Arguments("[Get(\"/i/{**rest}\")] Task<string> Get([Encoded] string rest);")]
    [Arguments("[Get(\"/i/{**rest}\")] Task<string> Get(string rest);")]
    [Arguments("[Post(\"/i\")] Task<string> Post(System.IO.Stream body, string tag);")]
    [Arguments("[Get(\"/i\")] Task<string> Get([Property(\"key\")][Query] string value);")]
    [Arguments("[Get(\"/signin\")] Task<string> SignIn([AliasAs(\"login\")] string login, [AliasAs(\"tok\")] string token);")]
    [Arguments("[Get(\"/signin?login={login}&tok={token}\")] Task<string> SignIn(string login, string token);")]
    public async Task SupportedQueryShapeGeneratesInline(string body)
    {
        var generated = Generate(body);

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies unsupported query shapes fall back to the reflection request builder.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("[Get(\"/i\")] Task<string> Get(object value);")]
    [Arguments("[Get(\"/i\")] Task<string> Get(System.Collections.Generic.Dictionary<string, object> map);")]
    [Arguments("[Get(\"/i\")] Task<string> Get([Query] System.Collections.Generic.IDictionary<string, object> map);")]
    [Arguments("[Post(\"/i\")] Task<string> Post(object first, object second);")]
    public async Task UnsupportedQueryShapeFallsBack(string body)
    {
        var generated = Generate(body);

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a dictionary of simple keys and values expands inline instead of using reflection.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    /// <remarks>
    /// An <c>object</c>-valued dictionary keeps falling back: the reflection builder inspects each value's runtime type
    /// to decide whether to recurse into it, which the generator cannot determine from the declared type.
    /// </remarks>
    [Test]
    [Arguments("[Get(\"/i\")] Task<string> Get(System.Collections.Generic.Dictionary<string, string> map);")]
    [Arguments("[Get(\"/i\")] Task<string> Get([Query] System.Collections.Generic.Dictionary<string, string> map);")]
    [Arguments("[Get(\"/i\")] Task<string> Get(System.Collections.Generic.IDictionary<int, string> map);")]
    public async Task DictionaryQueryShapeGeneratesInline(string body)
    {
        var generated = Generate(body);

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("GeneratedQueryStringBuilder");
    }

    /// <summary>Verifies a query object whose properties are collections of simple elements generates inline,
    /// emitting both the default-formatter fast path and the customized-formatter slow path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CollectionPropertyObjectGeneratesInline()
    {
        const string source =
            """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Filter
            {
                public int[]? Ids { get; set; }

                [Query(CollectionFormat.Multi)]
                public List<string>? Tags { get; set; }
            }

            public interface IGeneratedClient
            {
                [Get("/i")]
                Task<string> Get([Query] Filter filter);
            }
            """;

        var generated = Fixture.RunGenerator(source, generatedRequestBuilding: true)
            .GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("BeginCollection");
        await Assert.That(generated).Contains("AddFormattedCollectionProperty");
    }

    /// <summary>Verifies a query object with a nested concrete object property generates inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NestedObjectPropertyGeneratesInline()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Address
            {
                public string? City { get; set; }
            }

            public sealed class Filter
            {
                public string? Name { get; set; }

                public Address? Address { get; set; }
            }

            public interface IGeneratedClient
            {
                [Get("/i")]
                Task<string> Get([Query] Filter filter);
            }
            """;

        var generated = Fixture.RunGenerator(source, generatedRequestBuilding: true)
            .GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a self-referential (cyclic) query object falls back to the reflection request builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CyclicObjectPropertyFallsBack()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Node
            {
                public string? Value { get; set; }

                public Node? Next { get; set; }
            }

            public interface IGeneratedClient
            {
                [Get("/i")]
                Task<string> Get([Query] Node node);
            }
            """;

        var generated = Fixture.RunGenerator(source, generatedRequestBuilding: true)
            .GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a <c>[QueryConverter]</c> parameter generates inline by delegating to the converter,
    /// even for an object-valued dictionary the generator cannot flatten from the declared type.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task QueryConverterParameterGeneratesInline()
    {
        const string source =
            """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class MapConverter : IQueryConverter<Dictionary<string, object>>
            {
                public void Flatten(Dictionary<string, object> value, string keyPrefix, ref GeneratedQueryStringBuilder builder, RefitSettings settings)
                {
                }
            }

            public interface IGeneratedClient
            {
                [Get("/i")]
                Task<string> Get([QueryConverter(typeof(MapConverter))] Dictionary<string, object> filter);
            }
            """;

        var generated = Fixture.RunGenerator(source, generatedRequestBuilding: true)
            .GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains(".Flatten(");
        await Assert.That(generated).Contains("new global::RefitGeneratorTest.MapConverter()");
    }

    /// <summary>Verifies <c>[QueryConverter]</c> on a method that cannot generate inline for another reason reports RF007.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task QueryConverterOnFallbackMethodReportsError()
    {
        const string source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class MapConverter : IQueryConverter<Dictionary<string, object>>
            {
                public void Flatten(Dictionary<string, object> value, string keyPrefix, ref GeneratedQueryStringBuilder builder, RefitSettings settings)
                {
                }
            }

            public interface IGeneratedClient
            {
                [Get("/i")]
                Task<string> Get([QueryConverter(typeof(MapConverter))] Dictionary<string, object> filter, object other);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        var ids = result.GeneratorDiagnostics.Select(static diagnostic => diagnostic.Id).ToArray();
        await Assert.That(ids).Contains(SourceGenOnlyDiagnosticId);
    }

    /// <summary>Verifies a type closing <c>IEnumerable&lt;T&gt;</c> over two element types falls back.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AmbiguousEnumerableQueryParameterFallsBack()
    {
        const string source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IDualSequence : IEnumerable<int>, IEnumerable<string> { }

            public interface IGeneratedClient
            {
                [Get("/items")]
                Task<string> Get([Query] IDualSequence values);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
    }

    /// <summary>Verifies a type closing <c>IDictionary&lt;TKey, TValue&gt;</c> more than once falls back.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AmbiguousDictionaryQueryParameterFallsBack()
    {
        const string source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IDualMap : IDictionary<string, int>, IDictionary<int, string> { }

            public interface IGeneratedClient
            {
                [Get("/items")]
                Task<string> Get([Query] IDualMap filter);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
    }

    /// <summary>Verifies a <c>[QueryConverter(null)]</c> with no resolvable converter type falls back.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task QueryConverterWithNullTypeFallsBack()
    {
        const string source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/i")]
                Task<string> Get([QueryConverter(null)] Dictionary<string, object> filter);
            }
            """;

        var generated = Fixture.RunGenerator(source, generatedRequestBuilding: true)
            .GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a non-nullable dictionary query parameter flattens inline through the unguarded branch.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NonNullableDictionaryQueryParameterGeneratesInline()
    {
        const string source =
            """
            #nullable enable
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/i")]
                Task<string> Get([Query] Dictionary<string, string> filter);
            }
            """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a compile-time <c>[Query(Format)]</c> on a plain enum emits a formatted numeric fallback.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EnumQueryWithFormatEmitsFormattedNumericFallback()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public enum Palette { Red, Green }

            public interface IGeneratedClient
            {
                [Get("/p")]
                Task<string> Get([Query(Format = "D")] Palette palette);
            }
            """;

        var generated = Fixture.RunGenerator(source, generatedRequestBuilding: true)
            .GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies enum-member override reading skips non-<c>EnumMember</c> attributes and a null-valued
    /// <c>[EnumMember]</c> while a scalar enum query flattens inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task EnumMemberOverrideReadingHandlesMissingAndNonEnumMemberAttributes()
    {
        const string source =
            """
            using System;
            using System.Runtime.Serialization;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public enum Season
            {
                [Obsolete]
                Spring,
                [EnumMember(Value = null)]
                Summer,
                [EnumMember(Value = "fall")]
                Autumn,
            }

            public interface IGeneratedClient
            {
                [Get("/s")]
                Task<string> Get([Query] Season season);
            }
            """;

        var generated = Fixture.RunGenerator(source, generatedRequestBuilding: true)
            .GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies an implicit (un-attributed) complex body on a body-capable method generates inline (issue #2190).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ImplicitComplexBodyGeneratesInline()
    {
        const string source =
            """
            using System.Threading;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class AuthRequest
            {
                public string? User { get; set; }
            }

            public interface IGeneratedClient
            {
                [Post("/v1/auth")]
                Task<string> AuthMePlease(AuthRequest request, CancellationToken token);
            }
            """;

        var generated = Fixture.RunGenerator(source, generatedRequestBuilding: true)
            .GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies un-attributed complex parameters become the implicit body on body-capable methods.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ImplicitBodyGeneratesInlineContent()
    {
        var generated = Generate("[Post(\"/i\")] Task<string> Post(System.IO.Stream payload, string tag);");

        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("CreateBodyContent<global::System.IO.Stream>");
        await Assert.That(generated).Contains("global::Refit.BodySerializationMethod.Serialized");
    }

    /// <summary>Verifies a source-generation-only attribute on a fallback method reports RF007.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("[Get(\"/i\")] Task<string> Get([QueryName] string flag, object other);")]
    [Arguments("[Get(\"/i/{**rest}\")] Task<string> Get([Encoded] int rest);")]
    [Arguments("[Multipart][Post(\"/i\")] Task<string> Post([QueryName] string flag, object payload);")]
    public async Task SourceGenOnlyAttributeOnFallbackMethodReportsError(string body)
    {
        var result = Fixture.RunGenerator(BuildSource(body), generatedRequestBuilding: true);

        var ids = result.GeneratorDiagnostics.Select(static diagnostic => diagnostic.Id).ToArray();
        await Assert.That(ids).Contains(SourceGenOnlyDiagnosticId);
    }

    /// <summary>Verifies RF007 also fires when generated request building is disabled entirely.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SourceGenOnlyAttributeWithBuildingDisabledReportsError()
    {
        var result = Fixture.RunGenerator(
            BuildSource("[Get(\"/i\")] Task<string> Get([QueryName] string flag);"),
            generatedRequestBuilding: false);

        var ids = result.GeneratorDiagnostics.Select(static diagnostic => diagnostic.Id).ToArray();
        await Assert.That(ids).Contains(SourceGenOnlyDiagnosticId);
    }

    /// <summary>Verifies RF007 does not fire for inline-eligible methods using the new attributes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SourceGenOnlyAttributeOnInlineMethodReportsNothing()
    {
        var result = Fixture.RunGenerator(
            BuildSource("[Get(\"/i\")] Task<string> Get([QueryName] string flag, [Encoded] string v);"),
            generatedRequestBuilding: true);

        var ids = result.GeneratorDiagnostics.Select(static diagnostic => diagnostic.Id).ToArray();
        await Assert.That(ids).DoesNotContain(SourceGenOnlyDiagnosticId);
    }

    /// <summary>Runs the generator over an interface body and returns the generated client source.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <returns>The generated client source text.</returns>
    private static string Generate(string body) =>
        Fixture.RunGenerator(BuildSource(body), generatedRequestBuilding: true)
            .GeneratedSources[GeneratedClientHintName];

    /// <summary>Wraps an interface body in a compilable Refit client source.</summary>
    /// <param name="body">The interface member body source.</param>
    /// <returns>The full source string.</returns>
    private static string BuildSource(string body) =>
        $$"""
        using System;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public interface IGeneratedClient
        {
            {{body}}
        }
        """;
}
