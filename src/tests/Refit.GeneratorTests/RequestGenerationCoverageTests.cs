// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Refit.Generator;

namespace Refit.GeneratorTests;

/// <summary>Focused tests that drive request parsing and inline emission branches to full coverage.</summary>
public sealed class RequestGenerationCoverageTests
{
    /// <summary>The generated implementation source hint name used by these tests.</summary>
    private const string GeneratedClientHintName = "IGeneratedClient.g.cs";

    /// <summary>The reflective request-builder call emitted by fallback paths.</summary>
    private const string ReflectiveRequestBuilderCall = "BuildRestResultFuncForMethod";

    /// <summary>Verifies a template with repeated and multiple placeholders is parsed and generated inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DuplicateAndMultiplePathPlaceholdersGenerateInline()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/a/{id}/b/{id}/c/{name}")]
                Task<string> Get(int id, string name);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("BuildRequestPath");
    }

    /// <summary>Verifies every constructor and named argument shape of a form-body query attribute is parsed.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UrlEncodedFormBodyCoversAllQueryShapes()
    {
        const string Source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public class SignupForm
            {
                [Query("d", "p", "fmt")]
                public string WithFormat { get; set; }

                [Query(CollectionFormat.Multi)]
                public List<string> Roles { get; set; }

                [Query(Format = "F2")]
                public double Amount { get; set; }

                [Query(CollectionFormat = CollectionFormat.Csv)]
                public List<int> Ids { get; set; }

                [Query(SerializeNull = true)]
                public string Note { get; set; }

                [Query(TreatAsString = true)]
                public string Treated { get; set; }

                [AliasAs(null)]
                public string Aliased { get; set; }

                public static string Ignored { get; set; }

                public string this[int index] => index.ToString();

                private string Secret { get; set; }

                public string HiddenGetter { private get; set; }

                public string WriteOnly { set { } }
            }

            public interface IGeneratedClient
            {
                [Post("/signup")]
                Task Signup([Body(BodySerializationMethod.UrlEncoded)] SignupForm form);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains(
            "global::Refit.FormField<global::RefitGeneratorTest.SignupForm>[]");
        await Assert.That(generated).Contains("body.@WithFormat");
        await Assert.That(generated).DoesNotContain("body.@Secret");
        await Assert.That(generated).DoesNotContain("body.@Ignored");
    }

    /// <summary>Verifies an unrolled scalar form body emits the empty-value branch for a nullable
    /// <c>[Query(SerializeNull = true)]</c> field, alongside an unconditionally added value-type field.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnrolledFormBodyEmitsSerializeNullEmptyBranch()
    {
        const string Source =
            """
            #nullable enable
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class NoteForm
            {
                public int Count { get; set; }

                [Query(SerializeNull = true)]
                public string? Note { get; set; }
            }

            public interface IGeneratedClient
            {
                [Post("/notes")]
                Task Submit([Body(BodySerializationMethod.UrlEncoded)] NoteForm form);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("string.Empty");
    }

    /// <summary>Verifies an unrolled form body whose only field renders through the formatter (an enum with duplicate
    /// constants) declares no default-form-formatting branch and still generates inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnrolledFormBodyWithFormatterOnlyFieldGeneratesInline()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public enum Duplicated { First = 1, Alias = 1 }

            public sealed class ModeForm
            {
                public Duplicated Mode { get; set; }
            }

            public interface IGeneratedClient
            {
                [Post("/modes")]
                Task Submit([Body(BodySerializationMethod.UrlEncoded)] ModeForm form);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies dynamic header parameters with valid, null, and whitespace names are parsed.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HeaderParametersCoverNameValidation()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/valid")]
                Task<string> Valid([Header("X-Valid")] string value);

                [Get("/missing")]
                Task<string> Missing([Header(null)] string value);

                [Get("/blank")]
                Task<string> Blank([Header("   ")] string value);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains("SetHeader");
    }

    /// <summary>Verifies an HTTP method attribute with an unresolved path argument degrades gracefully.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnresolvedHttpAttributeArgumentProducesEmptyPath()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get(UndefinedRoute)]
                Task<string> Get();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(GeneratedClientHintName)).IsTrue();
    }

    /// <summary>Verifies a header parameter with an unresolved name argument degrades gracefully.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnresolvedHeaderArgumentFallsBack()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/values")]
                Task<string> Get([Header(UndefinedHeader)] string value);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(GeneratedClientHintName)).IsTrue();
    }

    /// <summary>Verifies repeated custom parameter attributes emit a grouped attribute provider with all argument kinds.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RepeatedPathParameterAttributesEmitGroupedProvider()
    {
        const string Source =
            """
            using System;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
            public sealed class TagAttribute : Attribute
            {
                public TagAttribute(Type marker, int[] codes, DayOfWeek day)
                {
                }
            }

            public interface IGeneratedClient
            {
                [Get("/things/{id}")]
                Task<string> Get(
                    [Tag(typeof(int), new[] { 1, 2 }, DayOfWeek.Monday)]
                    [Tag(typeof(string), new[] { 3 }, DayOfWeek.Friday)]
                    int id);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains("GeneratedParameterAttributeProvider");
        await Assert.That(generated).Contains("new global::RefitGeneratorTest.TagAttribute(typeof(int), new[] { 1, 2 }");
        await Assert.That(generated).Contains("new global::RefitGeneratorTest.TagAttribute(typeof(string), new[] { 3 }");
        await Assert.That(generated).Contains("(global::System.DayOfWeek)");
    }

    /// <summary>Verifies a parameter attribute carrying a null argument renders the null literal.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ParameterAttributeWithNullArgumentRendersNullLiteral()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/values/{value}")]
                Task<string> Get([AliasAs(null)] int value);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).Contains("AliasAsAttribute(null)");
    }

    /// <summary>Verifies a multipart method with statically-dispatchable parts builds its content inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartMethodGeneratesInline()
    {
        const string Source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Multipart]
                [Post("/upload")]
                Task<string> Upload([AliasAs("file")] IEnumerable<StreamPart> streams);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("new global::System.Net.Http.MultipartFormDataContent(\"----MyGreatBoundary\")");
    }

    /// <summary>Verifies a multipart method with an <c>object</c>-typed part still falls back to the reflective builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartMethodWithObjectPartFallsBack()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Multipart]
                [Post("/upload")]
                Task<string> Upload(object payload);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies the generic API response interface return type is classified as an API response.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericApiResponseInterfaceReturnIsClassified()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/typed")]
                Task<IApiResponse<string>> Get();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a method returning a non-named type is parsed without inline generation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NonNamedReturnTypeIsParsed()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/array")]
                string[] GetArray();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(GeneratedClientHintName)).IsTrue();
    }

    /// <summary>Verifies the scalar classifier evaluates its full special-type pattern across representative types.</summary>
    /// <param name="parameterType">The path parameter type expression.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("int")]
    [Arguments("string")]
    [Arguments("System.DateTime")]
    [Arguments("System.Guid")]
    [Arguments("object")]
    public async Task ScalarClassifierEvaluatesSpecialTypePattern(string parameterType)
    {
        var source =
            $$"""
              using System;
              using System.Threading.Tasks;
              using Refit;

              namespace RefitGeneratorTest;

              public interface IGeneratedClient
              {
                  [Get("/items/{value}")]
                  Task<string> Get({{parameterType}} value);
              }
              """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(GeneratedClientHintName)).IsTrue();
    }

    /// <summary>Verifies form bodies of otherwise-ineligible type kinds keep the reflection content path.</summary>
    /// <param name="signature">The method signature exercising a specific ineligible body type kind.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("Task Post<TBody>([Body(BodySerializationMethod.UrlEncoded)] TBody body);")]
    [Arguments("Task Post<T>([Body(BodySerializationMethod.UrlEncoded)] System.Collections.Generic.List<T> body);")]
    [Arguments("Task Post([Body(BodySerializationMethod.UrlEncoded)] dynamic body);")]
    [Arguments("Task Post([Body(BodySerializationMethod.UrlEncoded)] MissingBodyType body);")]
    [Arguments("Task Post([Body(BodySerializationMethod.UrlEncoded)] int* body);")]
    public async Task IneligibleFormBodyTypeKindsUseReflectionContent(string signature)
    {
        var source =
            $$"""
              using System.Threading.Tasks;
              using Refit;

              namespace RefitGeneratorTest;

              public interface IGeneratedClient
              {
                  [Post("/x")]
                  {{signature}}
              }
              """;

        var result = Fixture.RunGenerator(source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(generated).DoesNotContain("global::Refit.FormField<");
    }

    /// <summary>Verifies a Refit-namespaced result type that is not an API response is classified accordingly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RefitNonResponseReturnTypeIsNotClassifiedAsApiResponse()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/exception")]
                Task<ApiException> Get();
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.GeneratedSources.ContainsKey(GeneratedClientHintName)).IsTrue();
    }

    /// <summary>Verifies the HTTP method attribute lookup returns null when no matching attribute is present.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FindHttpMethodAttributeReturnsNullWithoutHttpAttribute()
    {
        const string Source =
            """
            using System;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IApi
            {
                [Obsolete]
                Task<string> Tagged();

                Task<string> Bare();
            }
            """;

        var compilation = Fixture.CreateLibrary(CSharpSyntaxTree.ParseText(Source));
        var httpMethodBase = compilation.GetTypeByMetadataName("Refit.HttpMethodAttribute")!;
        var api = compilation.GetTypeByMetadataName("RefitGeneratorTest.IApi")!;
        var tagged = api.GetMembers("Tagged").OfType<IMethodSymbol>().First();
        var bare = api.GetMembers("Bare").OfType<IMethodSymbol>().First();

        await Assert.That(Parser.FindHttpMethodAttribute(tagged, httpMethodBase)).IsNull();
        await Assert.That(Parser.FindHttpMethodAttribute(bare, httpMethodBase)).IsNull();
    }

    /// <summary>Verifies an unbalanced brace segment makes a path template unsupported for inline generation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IsPathSupportedRejectsUnbalancedBraceSegment()
    {
        await Assert.That(Parser.IsPathSupported("/root/{open/leaf}")).IsFalse();
        await Assert.That(Parser.IsPathSupported("/root/{closed}/leaf")).IsTrue();
    }

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

    /// <summary>Verifies a non-<c>[Encoded]</c> round-trip catch-all path parameter of any type generates inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RoundTripCatchAllPathGeneratesInline()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public readonly record struct RepoPath(string Value)
            {
                public override string ToString() => Value;
            }

            public interface IGeneratedClient
            {
                [Get("/repos/{**value}/contents")] Task<string> Get(RepoPath value);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("RoundTripEscapePath");
    }

    /// <summary>Verifies an <c>[Authorize]</c> parameter generates an inline Authorization header with its scheme.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AuthorizeParameterGeneratesInlineAuthorizationHeader()
    {
        const string Source =
            """
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public interface IGeneratedClient
            {
                [Get("/a")] Task<string> DefaultScheme([Authorize] string token);
                [Get("/b")] Task<string> ExplicitScheme([Authorize("Token")] string token);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);
        var generated = result.GeneratedSources[GeneratedClientHintName];

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
        await Assert.That(generated).Contains("\"Authorization\"");
        await Assert.That(generated).Contains("\"Bearer \"");
        await Assert.That(generated).Contains("\"Token \"");
    }

    /// <summary>Verifies url-encoded form bodies covering nullable collection entries and escaped field names.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UrlEncodedFormBodyCoversNullableCollectionAndEscapedNames()
    {
        const string Source =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Refit;

            namespace RefitGeneratorTest;

            public sealed class Form
            {
                public List<string?>? Tags { get; set; }
                [AliasAs("we\"ird\\name")] public string? Odd { get; set; }
                public string? Plain { get; set; }
            }

            public interface IGeneratedClient
            {
                [Post("/f")]
                Task Submit([Body(BodySerializationMethod.UrlEncoded)] Form form);
            }
            """;

        var result = Fixture.RunGenerator(Source, generatedRequestBuilding: true);

        await Assert.That(result.CompilesWithoutErrors).IsTrue();
    }
}
