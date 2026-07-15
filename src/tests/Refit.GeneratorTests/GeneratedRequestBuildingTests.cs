// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Generator tests for opt-in generated request construction.</summary>
public partial class GeneratedRequestBuildingTests
{
    /// <summary>The generated implementation source hint name used by these tests.</summary>
    private const string GeneratedClientHintName = "IGeneratedClient.g.cs";

    /// <summary>The reflective request-builder call emitted by fallback paths.</summary>
    private const string ReflectiveRequestBuilderCall = "BuildRestResultFuncForMethod";

    /// <summary>The generated request-runner send call emitted by inline request construction.</summary>
    private const string GeneratedRequestRunnerSendAsync = "GeneratedRequestRunner.SendAsync";

    /// <summary>The generated request-message construction emitted by inline request construction.</summary>
    private const string NewHttpRequestMessage = "new global::System.Net.Http.HttpRequestMessage";

    /// <summary>A simple eligible GET method used by switch tests.</summary>
    private const string SimpleGetMethod =
        """
        [Get("/users")]
        Task<string> Get(CancellationToken cancellationToken);
        """;

    /// <summary>Verifies the default output uses generated request construction for eligible methods.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DefaultUsesGeneratedRequestConstruction()
    {
        var generated = Fixture.GenerateForBody(
            SimpleGetMethod,
            GeneratedClientHintName);

        await Assert.That(generated).Contains(GeneratedRequestRunnerSendAsync);
        await Assert.That(generated).Contains(NewHttpRequestMessage);
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies an explicit switch-off keeps using the reflective request builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ExplicitSwitchOffUsesReflectiveRequestBuilder()
    {
        var generated = Fixture.GenerateForBody(
            SimpleGetMethod,
            GeneratedClientHintName,
            generatedRequestBuilding: false);

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
        await Assert.That(generated).DoesNotContain(GeneratedRequestRunnerSendAsync);
    }

    /// <summary>Verifies the legacy source-generator disable switch prevents all generated output.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DisableSourceGeneratorProducesNoSources()
    {
        var result = Fixture.RunGeneratorForBody(
            SimpleGetMethod,
            generatedRequestBuilding: null,
            disableSourceGenerator: true);

        await Assert.That(result.GeneratedSources).IsEmpty();
    }

    /// <summary>Verifies the explicit switch-on emits inline request construction for a simple eligible method.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsInlineRequestConstructionForSimpleMethod()
    {
        var generated = Fixture.GenerateForBody(
            SimpleGetMethod,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(GeneratedRequestRunnerSendAsync);
        await Assert.That(generated).Contains(NewHttpRequestMessage);
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a JSON Lines body parameter emits the JSON Lines body factory call.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsJsonLinesBodyContent()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Post("/import")]
            Task Import([Body(BodySerializationMethod.JsonLines)] IEnumerable<string> documents);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("global::Refit.GeneratedRequestRunner.CreateJsonLinesBodyContent");
    }

    /// <summary>Verifies an inherited Refit method is emitted through an explicit interface implementation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsInlineConstructionForDerivedRefitMethod()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            namespace RefitGeneratorTest;

            public interface IBaseClient
            {
                [Get("/base")]
                Task<string> GetBase();
            }

            public interface IDerivedClient : IBaseClient
            {
                [Get("/derived")]
                Task<string> GetDerived();
            }
            """,
            "IDerivedClient.g.cs",
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("global::RefitGeneratorTest.IBaseClient.GetBase");
        await Assert.That(generated).Contains(GeneratedRequestRunnerSendAsync);
    }

    /// <summary>Verifies generated inline request construction compiles in a consumer compilation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnGeneratedRequestConstructionCompiles()
    {
        var errors = Fixture.GenerateErrorsForBody(
            """
            [Post("/users")]
            Task<ApiResponse<string>> Create([Body] string name, CancellationToken cancellationToken);

            [Get("/users")]
            Task<string> HeaderValue([Header("X-Test")] int id);

            [Get("/users")]
            Task<string> HeaderReference([Header("X-Test")] string? id);

            [Get("/users")]
            Task<string> Headers([HeaderCollection] IDictionary<string, string> headers);

            [Get("/users")]
            Task<string> Property([Property("tenant")] int tenantId);

            string Client { get; set; }
            """,
            generatedRequestBuilding: true);

        var errorMessages = string.Join(Environment.NewLine, errors.Select(static diagnostic => diagnostic.ToString()));
        await Assert.That(errorMessages).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies a parameter named like a generated local does not collide (issue #2161).</summary>
    /// <param name="parameterName">A parameter name matching a generated local variable.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("refitSettings")]
    [Arguments("refitRequest")]
    [Arguments("refitArguments")]
    [Arguments("refitRequestBuilder")]
    [Arguments("refitFunc")]
    public async Task ParameterNamedLikeGeneratedLocalCompiles(string parameterName)
    {
        var body = $$"""
            [Post("/todos")]
            Task<string> CreateAsync([Body] string {{parameterName}});
            """;

        foreach (var generatedRequestBuilding in new bool?[] { true, false })
        {
            var errors = Fixture.GenerateErrorsForBody(body, generatedRequestBuilding);
            var errorMessages = string.Join(Environment.NewLine, errors.Select(static diagnostic => diagnostic.ToString()));
            await Assert.That(errorMessages).IsEqualTo(string.Empty);
        }
    }

    /// <summary>Verifies static headers are emitted into inline request construction.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsStaticHeaders()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Headers("X-Test: test")]
            [Get("/users")]
            Task<string> Get(CancellationToken cancellationToken);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("GeneratedRequestRunner.SetHeader(refitRequest, \"X-Test\", \"test\", refitSettings.ValidateHeaders)");
        await Assert.That(generated).Contains(GeneratedRequestRunnerSendAsync);
    }

    /// <summary>Verifies constant inline paths strip URI fragments before request construction.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnStripsFragmentsFromInlineConstantPaths()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/foo?key=value#name")]
            Task<string> Get();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("BuildRelativeUri(this.Client, \"/foo?key=value\"");
        await Assert.That(generated).DoesNotContain("#name");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies constant inline paths discard query text after a fragment marker.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnStripsQueryAfterFragmentFromInlineConstantPaths()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/foo#?key=value")]
            Task<string> Get();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("BuildRelativeUri(this.Client, \"/foo\"");
        await Assert.That(generated).DoesNotContain("?key=value");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies constant inline paths remove empty query keys to match runtime builder semantics.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnRemovesEmptyQueryKeysFromInlineConstantPaths()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/foo?=drop&key=&two=2")]
            Task<string> Get();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("BuildRelativeUri(this.Client, \"/foo?key=&two=2\"");
        await Assert.That(generated).DoesNotContain("=drop");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies legacy JSON body metadata emits the non-obsolete equivalent enum member.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnMapsLegacyJsonBodySerializationToSerialized()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Post("/users")]
            Task<string> Create([Body(BodySerializationMethod.Json)] string name);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("BodySerializationMethod.Serialized");
        await Assert.That(generated).DoesNotContain("BodySerializationMethod.Json");
    }

    /// <summary>Verifies dynamic header parameters are emitted into inline request construction.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsDynamicHeader()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/users")]
            Task<string> Get([Header("X-Test")] int id);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("GeneratedRequestRunner.SetHeader(refitRequest, \"X-Test\", @id.ToString(), refitSettings.ValidateHeaders)");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies header collections are emitted into inline request construction.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsHeaderCollection()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/users")]
            Task<string> Get([HeaderCollection] IDictionary<string, string> headers);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("GeneratedRequestRunner.AddHeaderCollection(refitRequest, @headers, refitSettings.ValidateHeaders)");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies all built-in HTTP method attributes can be emitted inline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsAllBuiltInHttpMethodsInline()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Delete("/delete")]
            Task<string> Delete();

            [Head("/head")]
            Task<string> Head();

            [Options("/options")]
            Task<string> Options();

            [Patch("/patch")]
            Task<string> Patch();

            [Post("/post")]
            Task<string> Post();

            [Put("/put")]
            Task<string> Put();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("HttpMethod.Delete");
        await Assert.That(generated).Contains("HttpMethod.Head");
        await Assert.That(generated).Contains("HttpMethod.Options");
        await Assert.That(generated).Contains("new global::System.Net.Http.HttpMethod(\"PATCH\")");
        await Assert.That(generated).Contains("HttpMethod.Post");
        await Assert.That(generated).Contains("HttpMethod.Put");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies built-in HTTP method attributes are discovered when written with qualified names.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnDiscoversQualifiedBuiltInHttpMethodAttributes()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            using R = Refit;

            public interface IGeneratedClient
            {
                [global::Refit.GetAttribute("/global")]
                Task<string> Global();

                [R.PostAttribute("/alias")]
                Task<string> Alias();

                [Refit.Put("/qualified")]
                Task<string> Qualified();
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("BuildRelativeUri(this.Client, \"/global\"");
        await Assert.That(generated).Contains("BuildRelativeUri(this.Client, \"/alias\"");
        await Assert.That(generated).Contains("BuildRelativeUri(this.Client, \"/qualified\"");
        await Assert.That(generated).Contains("HttpMethod.Get");
        await Assert.That(generated).Contains("HttpMethod.Post");
        await Assert.That(generated).Contains("HttpMethod.Put");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies string literal escaping in inline request paths and header values.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEscapesInlineStringLiterals()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Headers("X-Quote: value\"with\\slashes\tand\nlines")]
            [Get("/escaped?name=value\"with\tand")]
            Task<string> Escaped();
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("\\\"with\\\\slashes\\tand");
        await Assert.That(generated).Contains("and\\nlines");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }
}
