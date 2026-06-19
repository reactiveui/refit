// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.GeneratorTests;

/// <summary>Generator tests for opt-in generated request construction.</summary>
public class GeneratedRequestBuildingTests
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

        var errorMessages = string.Join(Environment.NewLine, errors.Select(diagnostic => diagnostic.ToString()));
        await Assert.That(errorMessages).IsEqualTo(string.Empty);
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

        await Assert.That(generated).Contains("GeneratedRequestRunner.SetHeader(______rq, \"X-Test\", \"test\")");
        await Assert.That(generated).Contains(GeneratedRequestRunnerSendAsync);
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

        await Assert.That(generated).Contains("GeneratedRequestRunner.SetHeader(______rq, \"X-Test\", @id.ToString())");
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

        await Assert.That(generated).Contains("GeneratedRequestRunner.AddHeaderCollection(______rq, @headers)");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies invalid header collection semantics fall back to the runtime builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnFallsBackForInvalidHeaderCollection()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/users")]
            Task<string> Get([HeaderCollection] IDictionary<string, object> headers);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains(ReflectiveRequestBuilderCall);
        await Assert.That(generated).DoesNotContain("GeneratedRequestRunner.AddHeaderCollection");
    }

    /// <summary>Verifies request property parameters use typed generated helper calls.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsTypedPropertyParameter()
    {
        var generated = Fixture.GenerateForBody(
            """
            [Get("/users")]
            Task<string> Get([Property("tenant")] int tenantId);
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("GeneratedRequestRunner.AddRequestProperty<int>(______rq, \"tenant\", @tenantId)");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies property attributes on interface properties are implemented and passed into generated requests.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SwitchOnEmitsInterfacePropertyRequestProperty()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IGeneratedClient
            {
                [Property("tenant")]
                int TenantId { get; set; }

                [Get("/users")]
                Task<string> Get();
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("public int TenantId {  get; set; }");
        await Assert.That(generated).Contains("GeneratedRequestRunner.AddRequestProperty<int>(______rq, \"tenant\", this.TenantId)");
        await Assert.That(generated).DoesNotContain(ReflectiveRequestBuilderCall);
    }

    /// <summary>Verifies a get-only HttpClient Client interface property is satisfied by the generated infrastructure property.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DoesNotReemitGeneratedClientProperty()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IGeneratedClient
            {
                HttpClient Client { get; }

                [Get("/users")]
                Task<string> Get();
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        var clientPropertyCount = generated.Split("public global::System.Net.Http.HttpClient Client").Length - 1;
        await Assert.That(clientPropertyCount).IsEqualTo(1);
    }

    /// <summary>Verifies inherited non-Refit interface properties are implemented explicitly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ImplementsInheritedRegularInterfaceProperty()
    {
        var generated = Fixture.GenerateForDeclaration(
            """
            public interface IBaseApi
            {
                string BaseUri { get; set; }
            }

            public interface IGeneratedClient : IBaseApi
            {
                [Get("/users")]
                Task<string> Get();
            }
            """,
            GeneratedClientHintName,
            generatedRequestBuilding: true);

        await Assert.That(generated).Contains("global::IBaseApi.BaseUri");
        await Assert.That(generated).DoesNotContain("Either this method has no Refit HTTP method attribute");
    }
}
