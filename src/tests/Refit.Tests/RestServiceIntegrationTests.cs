// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Integration tests that exercise <see cref="RestService"/> end to end against a mock HTTP handler.</summary>
public partial class RestServiceIntegrationTests
{
    /// <summary>Base URL used by the mock HTTP handler exchanges.</summary>
    private const string BaseUrl = "http://foo";

    /// <summary>Query key used by the decimal query-parameter exchanges.</summary>
    private const string DecimalQueryKey = "value";

    /// <summary>Base URL including a trailing slash.</summary>
    private const string BaseUrlWithSlash = "http://foo/";

    /// <summary>URL for the trailing-slash trimming endpoint.</summary>
    private const string SomeEndpointUrl = "http://foo/someendpoint";

    /// <summary>URL for the path-bound foos/bar endpoint.</summary>
    private const string FoosBarBarNoneUrl = "http://foo/foos/1/bar/barNone";

    /// <summary>Sample path-bound property value.</summary>
    private const string BarNoneValue = "barNone";

    /// <summary>Sample path-bound foo identifier.</summary>
    private const int FooId = 22;

    /// <summary>Second sample path-bound collection value.</summary>
    private const int SecondFooValue = 23;

    /// <summary>Sample large path-bound foo identifier.</summary>
    private const int LargeFooId = 12_345;

    /// <summary>Stub endpoint URL returning a value body.</summary>
    private const string FooValueUrl = "http://foo/value";

    /// <summary>Stub endpoint URL used by the no-body request tests.</summary>
    private const string FooNobodyUrl = "http://foo/nobody";

    /// <summary>Query key asserted by the dynamic query-parameter tests.</summary>
    private const string SomeProperty2Key = "SomeProperty2";

    /// <summary>Stub endpoint URL used by the nested-path POST tests.</summary>
    private const string Foos22BarBarUrl = "http://foo/foos/22/bar/bar";

    /// <summary>Relative path to the sample PDF used by the file-upload tests.</summary>
    private const string TestFilePath = "Test Files/Test.pdf";

    /// <summary>File name used for the sample PDF multipart part.</summary>
    private const string TestFileName = "Test.pdf";

    /// <summary>Media type used for the sample PDF multipart part.</summary>
    private const string PdfMediaType = "application/pdf";

    /// <summary>JSON serializer options that apply the camelCase property naming policy.</summary>
    private static readonly JsonSerializerOptions _camelCaseJsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Refit interface used to verify Type-based generated factory lookup.</summary>
    [SuppressMessage(
        "RoslynCommonAnalyzers",
        "SST1437:Add members to type or remove it",
        Justification = "Empty fixture interface verifies manual Type-based generated factory registration.")]
    internal interface IGeneratedTypeFactoryApi;

    /// <summary>Refit interface used to verify the typed request-builder factory fallback.</summary>
    [SuppressMessage(
        "RoslynCommonAnalyzers",
        "SST1437:Add members to type or remove it",
        Justification = "Empty fixture interface verifies direct typed generated factory registration.")]
    internal interface IGeneratedTypedFactoryApi;

    /// <summary>Refit interface used to verify the settings dictionary factory fallback.</summary>
    [SuppressMessage(
        "RoslynCommonAnalyzers",
        "SST1437:Add members to type or remove it",
        Justification = "Empty fixture interface verifies generated settings factory fallback behavior.")]
    internal interface IGeneratedTypedSettingsFallbackApi;

    /// <summary>Interface intentionally excluded from source generation.</summary>
    [SuppressMessage(
        "RoslynCommonAnalyzers",
        "SST1437:Add members to type or remove it",
        Justification = "Empty fixture interface verifies missing generated factory behavior.")]
    private interface INotGeneratedApi;

#if NETCOREAPP3_1_OR_GREATER
    /// <summary>Verifies an instance can be created via the interface's static factory method.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CanCreateInstanceUsingStaticMethod()
    {
        var instance = IRefitInterfaceWithStaticMethod.Create();

        await Assert.That(instance).IsNotNull();
    }
#endif

    /// <summary>Verifies a registered generated factory is used to construct the implementation.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UsesRegisteredGeneratedFactory()
    {
        RestService.RegisterGeneratedFactory(
            typeof(IGeneratedFactoryApi),
            static (client, builder) => new GeneratedFactoryApiClient(client, builder));

        using var client = new HttpClient { BaseAddress = new(BaseUrl) };

        var instance = RestService.For<IGeneratedFactoryApi>(client);
        var generated = await Assert.That(instance).IsTypeOf<GeneratedFactoryApiClient>();

        await Assert.That(generated!.Client).IsSameReferenceAs(client);
        await Assert.That(generated.Builder).IsNotNull();
    }

    /// <summary>Verifies the generated-only API creates clients without a reflection request builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ForGeneratedUsesRegisteredFactoryWithoutReflectionBuilder()
    {
        RestService.RegisterGeneratedFactory(
            typeof(IGeneratedFactoryApi),
            static (client, builder) => new GeneratedFactoryApiClient(client, builder));

        using var client = new HttpClient { BaseAddress = new(BaseUrl) };
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());

        var instance = RestService.ForGenerated<IGeneratedFactoryApi>(client, settings);
        var generated = await Assert.That(instance).IsTypeOf<GeneratedFactoryApiClient>();

        await Assert.That(generated!.Client).IsSameReferenceAs(client);
        await Assert.That(generated.Builder.Settings).IsSameReferenceAs(settings);
        await Assert.That(
                () => generated.Builder.BuildRestResultFuncForMethod(nameof(IGeneratedFactoryApi.Get)))
            .ThrowsExactly<NotSupportedException>();
    }

    /// <summary>Verifies the generated-only API prefers settings factories when they are registered.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ForGeneratedUsesRegisteredSettingsFactory()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));

        using var client = new HttpClient { BaseAddress = new(BaseUrl) };
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());

        var instance = RestService.ForGenerated<IGeneratedSettingsFactoryApi>(client, settings);
        var generated = await Assert.That(instance).IsTypeOf<GeneratedSettingsFactoryApiClient>();

        await Assert.That(generated!.Client).IsSameReferenceAs(client);
        await Assert.That(generated.Settings).IsSameReferenceAs(settings);

        var generatedApiType = typeof(IGeneratedSettingsFactoryApi);
        var typedInstance = RestService.ForGenerated(generatedApiType, client, settings);
        var typedGenerated = await Assert.That(typedInstance).IsTypeOf<GeneratedSettingsFactoryApiClient>();

        await Assert.That(typedGenerated!.Client).IsSameReferenceAs(client);
        await Assert.That(typedGenerated.Settings).IsSameReferenceAs(settings);
    }

    /// <summary>Verifies generated-only overloads create clients from host URLs.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "This test intentionally covers the Type-based generated-client overload.")]
    public async Task ForGeneratedHostUrlOverloadsCreateRegisteredSettingsClients()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));

        var settings = new RefitSettings(new SystemTextJsonContentSerializer());

        var defaultSettingsInstance = RestService.ForGenerated<IGeneratedSettingsFactoryApi>(BaseUrlWithSlash);
        var explicitSettingsInstance = RestService.ForGenerated<IGeneratedSettingsFactoryApi>("http://bar/", settings);
        var typedInstance = RestService.ForGenerated(typeof(IGeneratedSettingsFactoryApi), "http://baz/", settings);

        var defaultGenerated = await Assert.That(defaultSettingsInstance).IsTypeOf<GeneratedSettingsFactoryApiClient>();
        var explicitGenerated = await Assert.That(explicitSettingsInstance).IsTypeOf<GeneratedSettingsFactoryApiClient>();
        var typedGenerated = await Assert.That(typedInstance).IsTypeOf<GeneratedSettingsFactoryApiClient>();

        await Assert.That(defaultGenerated!.Client.BaseAddress).IsEqualTo(new(BaseUrl));
        await Assert.That(explicitGenerated!.Client.BaseAddress).IsEqualTo(new("http://bar"));
        await Assert.That(explicitGenerated.Settings).IsSameReferenceAs(settings);
        await Assert.That(typedGenerated!.Client.BaseAddress).IsEqualTo(new("http://baz"));
        await Assert.That(typedGenerated.Settings).IsSameReferenceAs(settings);
    }

    /// <summary>Verifies generated-only Type overloads use request-builder factories and fail clearly when absent.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "This test intentionally covers the Type-based generated-client overload.")]
    public async Task ForGeneratedTypeOverloadsUseRegisteredRequestBuilderFactories()
    {
        RestService.RegisterGeneratedFactory(
            typeof(IGeneratedTypeFactoryApi),
            static (client, builder) => new GeneratedTypeFactoryApiClient(client, builder));

        using var client = new HttpClient { BaseAddress = new(BaseUrl) };
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());

        var instance = RestService.ForGenerated(typeof(IGeneratedTypeFactoryApi), client, settings);
        var generated = await Assert.That(instance).IsTypeOf<GeneratedTypeFactoryApiClient>();

        await Assert.That(generated!.Client).IsSameReferenceAs(client);
        await Assert.That(generated.Builder.Settings).IsSameReferenceAs(settings);
        await Assert.That(
                () => generated.Builder.BuildRestResultFuncForMethod("Get"))
            .ThrowsExactly<NotSupportedException>();

        var exception = await Assert
            .That(() => RestService.ForGenerated(typeof(INotGeneratedApi), client, settings))
            .ThrowsExactly<InvalidOperationException>();

        await Assert.That(exception!.Message).Contains("source generator is installed");
    }

    /// <summary>Verifies convenience overloads delegate to generated factory registrations.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "This test intentionally covers the Type-based reflection overload.")]
    public async Task GeneratedConvenienceOverloadsDelegateToRegisteredFactories()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedSettingsFactoryApi>(
            static (client, settings) => new GeneratedSettingsFactoryApiClient(client, settings));
        RestService.RegisterGeneratedFactory(
            typeof(IGeneratedFactoryApi),
            static (client, builder) => new GeneratedFactoryApiClient(client, builder));

        using var generatedClient = new HttpClient { BaseAddress = new("http://settings") };
        using var reflectionClient = new HttpClient { BaseAddress = new("http://factory") };

        var generatedOnly = RestService.ForGenerated<IGeneratedSettingsFactoryApi>(generatedClient);
        var reflectionPath = RestService.For(typeof(IGeneratedFactoryApi), reflectionClient);

        var generatedOnlyClient = await Assert.That(generatedOnly).IsTypeOf<GeneratedSettingsFactoryApiClient>();
        var reflectionClientInstance = await Assert.That(reflectionPath).IsTypeOf<GeneratedFactoryApiClient>();

        await Assert.That(generatedOnlyClient!.Client).IsSameReferenceAs(generatedClient);
        await Assert.That(reflectionClientInstance!.Client).IsSameReferenceAs(reflectionClient);
    }

    /// <summary>Verifies typed generated request-builder factories can be invoked without dictionary registration.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ForGeneratedUsesTypedRequestBuilderFactoryFallback()
    {
        var previousFactory = RestService.GeneratedFactory<IGeneratedTypedFactoryApi>.Factory;
        RestService.GeneratedFactory<IGeneratedTypedFactoryApi>.Factory =
            static (client, builder) => new GeneratedTypedFactoryApiClient(client, builder);

        try
        {
            using var client = new HttpClient { BaseAddress = new("http://typed") };
            var settings = new RefitSettings(new SystemTextJsonContentSerializer());

            var instance = RestService.ForGenerated<IGeneratedTypedFactoryApi>(client, settings);
            var generated = await Assert.That(instance).IsTypeOf<GeneratedTypedFactoryApiClient>();

            await Assert.That(generated!.Client).IsSameReferenceAs(client);
            await Assert.That(generated.Builder.Settings).IsSameReferenceAs(settings);
        }
        finally
        {
            RestService.GeneratedFactory<IGeneratedTypedFactoryApi>.Factory = previousFactory;
        }
    }

    /// <summary>Verifies registered settings factories remain usable through the untyped registry.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ForGeneratedUsesUntypedSettingsFactoryFallback()
    {
        RestService.RegisterGeneratedSettingsFactory<IGeneratedTypedSettingsFallbackApi>(
            static (client, settings) => new GeneratedTypedSettingsFallbackApiClient(client, settings));
        var previousFactory = RestService.GeneratedSettingsFactory<IGeneratedTypedSettingsFallbackApi>.Factory;
        RestService.GeneratedSettingsFactory<IGeneratedTypedSettingsFallbackApi>.Factory = null;

        try
        {
            using var client = new HttpClient { BaseAddress = new("http://typed-settings") };
            var settings = new RefitSettings(new SystemTextJsonContentSerializer());

            var instance = RestService.ForGenerated<IGeneratedTypedSettingsFallbackApi>(client, settings);
            var generated = await Assert.That(instance).IsTypeOf<GeneratedTypedSettingsFallbackApiClient>();

            await Assert.That(generated!.Client).IsSameReferenceAs(client);
            await Assert.That(generated.Settings).IsSameReferenceAs(settings);
        }
        finally
        {
            RestService.GeneratedSettingsFactory<IGeneratedTypedSettingsFallbackApi>.Factory = previousFactory;
        }
    }

    /// <summary>Verifies the generated-only API fails clearly when no generated factory is available.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ForGeneratedRequiresRegisteredFactory()
    {
        using var client = new HttpClient { BaseAddress = new(BaseUrl) };
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());

        var exception = await Assert
            .That(() => RestService.ForGenerated<INotGeneratedApi>(client, settings))
            .ThrowsExactly<InvalidOperationException>();

        await Assert.That(exception!.Message).Contains("source generator is installed");
    }

    /// <summary>Verifies methods returning a <see cref="ValueTask{TResult}"/> work as expected.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValueTaskMethodsShouldWork()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(FooValueUrl),
                Reply.Text("test", "text/plain")
            },
        };
        var fixture = handler.CreateClient<IValueTaskApi>(BaseUrl);

        var result = await fixture.GetValue("value");

        await Assert.That(result).IsEqualTo("test");
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies an unmatched route placeholder is left in the URL verbatim when <see cref="RefitSettings.AllowUnmatchedRouteParameters"/> is set.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnmatchedRouteParameterIsLeftVerbatimWhenAllowed()
    {
        Uri? captured = null;
        var handler = new StubHttp
        {
            {
                new RouteMatcher
                {
                    Template = "*",
                    Reusable = true
                },
                Reply.From(request =>
                {
                    captured = request.RequestUri;
                    return new(HttpStatusCode.OK)
                    {
                        Content = new StringContent("test")
                    };
                })
            },
        };

        var settings = new RefitSettings
        {
            AllowUnmatchedRouteParameters = true,
            HttpMessageHandlerFactory = () => handler,
        };

        var fixture = RestService.For<IUrlNoMatchingParameters>(BaseUrl, settings);

        var result = await fixture.GetValue();

        await Assert.That(result).IsEqualTo("test");
        await Assert.That(captured).IsNotNull();
        await Assert.That(Uri.UnescapeDataString(captured!.ToString())).Contains("/{value}", StringComparison.Ordinal);
    }

    /// <summary>Verifies an unmatched route placeholder still throws by default.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnmatchedRouteParameterStillThrowsByDefault()
    {
        // With no parameters at all the interface generates inline, so the unmatched-placeholder check
        // now runs when the request is built rather than at client creation.
        var service = RestService.For<IUrlNoMatchingParameters>(BaseUrl);
        _ = await Assert.That(() => (Task)service.GetValue()).ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies methods returning a <see cref="ValueTask{TResult}"/> of an API response work.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValueTaskApiResponseMethodsShouldWork()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(FooValueUrl),
                Reply.Text("test", "text/plain")
            },
        };
        var fixture = handler.CreateClient<IValueTaskApiResponseApi>(BaseUrl);

        using var response = await fixture.GetValue("value");

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(response.Content).IsEqualTo("test");
        await Assert.That(response.RequestMessage!.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(response.RequestMessage.RequestUri?.ToString()).IsEqualTo(FooValueUrl);
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies content headers can be added to a POST with no body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CanAddContentHeadersToPostWithoutBody()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher
                {
                    Method = HttpMethod.Post,
                    Template = FooNobodyUrl,
                    Headers = [("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8")],
                    Body = string.Empty,
                    Where = static r => r.Content?.Headers.ContentLength == 0,
                },
                Reply.Json("Ok")
            },
        };
        var fixture = handler.CreateClient<IBodylessApi>(BaseUrl);

        await fixture.Post();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a GET with no parameters trims the trailing slash from the base URL.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithNoParametersTest()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = SomeEndpointUrl, ExactQuery = string.Empty },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<ITrimTrailingForwardSlashApi>(BaseUrl);

        await fixture.Get();
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a no-content response deserializes to the default value.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithNoContentResponseReturnsDefault()
    {
        var handler = new StubHttp
        {
            {
                Route.Get("http://foo/values"),
                Reply.Status(HttpStatusCode.NoContent)
            },
        };

        var fixture = handler.CreateClient<INoContentApi>(BaseUrl);

        var result = await fixture.GetValues();

        await Assert.That(result).IsNull();
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a no-content API response carries null content.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithNoContentApiResponseReturnsNullContent()
    {
        var handler = new StubHttp
        {
            {
                Route.Get("http://foo/values"),
                Reply.Status(HttpStatusCode.NoContent)
            },
        };

        var fixture = handler.CreateClient<INoContentApi>(BaseUrl);

        using var response = await fixture.GetValuesResponse();

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(response.Content).IsNull();
        await Assert.That(response.Error).IsNull();
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies the base address from an HttpClient matches the configured endpoint.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BaseAddressFromHttpClientMatchesTest()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = SomeEndpointUrl, ExactQuery = string.Empty },
                Reply.Json("Ok")
            },
        };

        var client = new HttpClient(handler) { BaseAddress = new(BaseUrl) };

        var fixture = RestService.For<ITrimTrailingForwardSlashApi>(client);

        await fixture.Get();
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a trailing slash on the HttpClient base address is handled.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BaseAddressWithTrailingSlashFromHttpClientMatchesTest()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = SomeEndpointUrl, ExactQuery = string.Empty },
                Reply.Json("Ok")
            },
        };

        var client = new HttpClient(handler) { BaseAddress = new(BaseUrlWithSlash) };

        var fixture = RestService.For<ITrimTrailingForwardSlashApi>(client);

        await fixture.Get();
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a request before fixture creation does not break trailing-slash handling.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BaseAddressWithTrailingSlashCalledBeforeFromHttpClientMatchesTest()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "http://foo/firstRequest", ExactQuery = string.Empty },
                Reply.Json("Ok")
            },
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = SomeEndpointUrl, ExactQuery = string.Empty },
                Reply.Json("Ok")
            },
        };

        var client = new HttpClient(handler) { BaseAddress = new(BaseUrlWithSlash) };

        _ = await client.GetAsync(new Uri("/firstRequest", UriKind.RelativeOrAbsolute));

        var fixture = RestService.For<ITrimTrailingForwardSlashApi>(client);

        await fixture.Get();
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a GET with no parameters works with a trailing slash in the base URL.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithNoParametersTestTrailingSlashInBase()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = SomeEndpointUrl, ExactQuery = string.Empty },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<ITrimTrailingForwardSlashApi>(BaseUrlWithSlash);

        await fixture.Get();
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies content is not automatically added to a GET request.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DoesntAddAutoAddContentToGetRequest()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = FooNobodyUrl, Where = static r => r.Content is null },
                Reply.Json("Ok")
            },
        };
        var fixture = handler.CreateClient<IBodylessApi>(BaseUrl);

        await fixture.Get();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies content is not automatically added to a HEAD request.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DoesntAddAutoAddContentToHeadRequest()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Head, Template = FooNobodyUrl, Where = static r => r.Content is null },
                Reply.Json("Ok")
            },
        };
        var fixture = handler.CreateClient<IBodylessApi>(BaseUrl);

        await fixture.Head();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Opens the embedded test resource at the given relative path as a stream.</summary>
    /// <param name="relativeFilePath">The path of the embedded resource relative to the assembly root.</param>
    /// <returns>A stream over the matching embedded resource.</returns>
    internal static Stream GetTestFileStream(string relativeFilePath)
    {
        const char namespaceSeparator = '.';

        // get calling assembly
        var assembly = System.Reflection.Assembly.GetCallingAssembly();

        // compute resource name suffix
        var relativeName =
            "."
            + relativeFilePath
                .Replace('\\', namespaceSeparator)
                .Replace('/', namespaceSeparator)
                .Replace(' ', '_');

        // get resource stream
        var fullName = Array.Find(
            assembly.GetManifestResourceNames(),
            name => name.EndsWith(relativeName, StringComparison.InvariantCulture))
            ?? throw new InvalidOperationException(
                $"Unable to find resource for path \"{relativeFilePath}\". Resource with name ending on \"{relativeName}\" was not found in assembly.");

        return assembly.GetManifestResourceStream(fullName)
            ?? throw new InvalidOperationException(
                $"Unable to find resource for path \"{relativeFilePath}\". Resource named \"{fullName}\" was not found in assembly.");
    }

    /// <summary>Asserts that the supplied stack trace string contains the expected substring.</summary>
    /// <param name="expectedSubstring">The substring expected to appear in the stack trace.</param>
    /// <param name="actualString">The actual stack trace string to inspect.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task AssertStackTraceContains(string expectedSubstring, string? actualString) => await Assert.That(actualString).Contains(expectedSubstring);

    /// <summary>Hand-written generated implementation for Type-based generated factory tests.</summary>
    /// <param name="client">The HTTP client supplied by Refit.</param>
    /// <param name="builder">The request builder supplied by Refit.</param>
    private sealed class GeneratedTypeFactoryApiClient(HttpClient client, IRequestBuilder builder)
        : IGeneratedTypeFactoryApi
    {
        /// <summary>Gets the HTTP client supplied to the factory.</summary>
        public HttpClient Client { get; } = client;

        /// <summary>Gets the request builder supplied to the factory.</summary>
        public IRequestBuilder Builder { get; } = builder;
    }

    /// <summary>Client used to verify typed generated request-builder factories.</summary>
    /// <param name="client">The HTTP client supplied to the generated factory.</param>
    /// <param name="builder">The generated-only request builder supplied to the generated factory.</param>
    private sealed class GeneratedTypedFactoryApiClient(HttpClient client, IRequestBuilder builder)
        : IGeneratedTypedFactoryApi
    {
        /// <summary>Gets the HTTP client supplied to the generated factory.</summary>
        public HttpClient Client { get; } = client;

        /// <summary>Gets the generated-only request builder supplied to the generated factory.</summary>
        public IRequestBuilder Builder { get; } = builder;
    }

    /// <summary>Client used to verify generated settings factory fallbacks.</summary>
    /// <param name="client">The HTTP client supplied to the generated factory.</param>
    /// <param name="settings">The Refit settings supplied to the generated factory.</param>
    private sealed class GeneratedTypedSettingsFallbackApiClient(HttpClient client, RefitSettings settings)
        : IGeneratedTypedSettingsFallbackApi
    {
        /// <summary>Gets the HTTP client supplied to the generated factory.</summary>
        public HttpClient Client { get; } = client;

        /// <summary>Gets the settings supplied to the generated factory.</summary>
        public RefitSettings Settings { get; } = settings;
    }
}
