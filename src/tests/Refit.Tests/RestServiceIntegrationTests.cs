// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using RichardSzalay.MockHttp;

namespace Refit.Tests;

/// <summary>Integration tests that exercise <see cref="RestService"/> end to end against a mock HTTP handler.</summary>
public partial class RestServiceIntegrationTests
{
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

        using var client = new HttpClient { BaseAddress = new("http://foo") };

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

        using var client = new HttpClient { BaseAddress = new("http://foo") };
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

        using var client = new HttpClient { BaseAddress = new("http://foo") };
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

        var defaultSettingsInstance = RestService.ForGenerated<IGeneratedSettingsFactoryApi>("http://foo/");
        var explicitSettingsInstance = RestService.ForGenerated<IGeneratedSettingsFactoryApi>("http://bar/", settings);
        var typedInstance = RestService.ForGenerated(typeof(IGeneratedSettingsFactoryApi), "http://baz/", settings);

        var defaultGenerated = await Assert.That(defaultSettingsInstance).IsTypeOf<GeneratedSettingsFactoryApiClient>();
        var explicitGenerated = await Assert.That(explicitSettingsInstance).IsTypeOf<GeneratedSettingsFactoryApiClient>();
        var typedGenerated = await Assert.That(typedInstance).IsTypeOf<GeneratedSettingsFactoryApiClient>();

        await Assert.That(defaultGenerated!.Client.BaseAddress).IsEqualTo(new("http://foo"));
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

        using var client = new HttpClient { BaseAddress = new("http://foo") };
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
        using var client = new HttpClient { BaseAddress = new("http://foo") };
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
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp.Expect(HttpMethod.Get, "http://foo/value").Respond("text/plain", "test");

        var fixture = RestService.For<IValueTaskApi>("http://foo", settings);

        var result = await fixture.GetValue("value");

        await Assert.That(result).IsEqualTo("test");
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies an unmatched route placeholder is left in the URL verbatim when <see cref="RefitSettings.AllowUnmatchedRouteParameters"/> is set.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnmatchedRouteParameterIsLeftVerbatimWhenAllowed()
    {
        var mockHttp = new MockHttpMessageHandler();
        Uri? captured = null;
        _ = mockHttp
            .When("*")
            .Respond(request =>
            {
                captured = request.RequestUri;
                return new(HttpStatusCode.OK) { Content = new StringContent("test") };
            });

        var settings = new RefitSettings
        {
            AllowUnmatchedRouteParameters = true,
            HttpMessageHandlerFactory = () => mockHttp,
        };

        var fixture = RestService.For<IUrlNoMatchingParameters>("http://foo", settings);

        var result = await fixture.GetValue();

        await Assert.That(result).IsEqualTo("test");
        await Assert.That(captured).IsNotNull();
        await Assert.That(Uri.UnescapeDataString(captured!.ToString())).Contains("/{value}", StringComparison.Ordinal);
    }

    /// <summary>Verifies an unmatched route placeholder still throws by default.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UnmatchedRouteParameterStillThrowsByDefault() =>
        await Assert.That(() => RestService.For<IUrlNoMatchingParameters>("http://foo")).ThrowsExactly<ArgumentException>();

    /// <summary>Verifies methods returning a <see cref="ValueTask{TResult}"/> of an API response work.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValueTaskApiResponseMethodsShouldWork()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp.Expect(HttpMethod.Get, "http://foo/value").Respond("text/plain", "test");

        var fixture = RestService.For<IValueTaskApiResponseApi>("http://foo", settings);

        using var response = await fixture.GetValue("value");

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(response.Content).IsEqualTo("test");
        await Assert.That(response.RequestMessage!.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(response.RequestMessage.RequestUri?.ToString()).IsEqualTo("http://foo/value");
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies content headers can be added to a POST with no body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CanAddContentHeadersToPostWithoutBody()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Post, "http://foo/nobody")

            // The content length header is set automatically by the HttpContent instance,
            // so checking the header as a string doesn't work
            .With(r => r.Content?.Headers.ContentLength == 0)

            // But we added content type ourselves, so this should work
            .WithHeaders("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8")
            .WithContent(string.Empty)
            .Respond("application/json", "Ok");

        var fixture = RestService.For<IBodylessApi>("http://foo", settings);

        await fixture.Post();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a GET with no parameters trims the trailing slash from the base URL.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithNoParametersTest()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/someendpoint")
            .WithExactQueryString(string.Empty)
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<ITrimTrailingForwardSlashApi>("http://foo", settings);

        await fixture.Get();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a no-content response deserializes to the default value.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithNoContentResponseReturnsDefault()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp.Expect(HttpMethod.Get, "http://foo/values").Respond(HttpStatusCode.NoContent);

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<INoContentApi>("http://foo", settings);

        var result = await fixture.GetValues();

        await Assert.That(result).IsNull();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a no-content API response carries null content.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithNoContentApiResponseReturnsNullContent()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp.Expect(HttpMethod.Get, "http://foo/values").Respond(HttpStatusCode.NoContent);

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<INoContentApi>("http://foo", settings);

        using var response = await fixture.GetValuesResponse();

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(response.Content).IsNull();
        await Assert.That(response.Error).IsNull();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies the base address from an HttpClient matches the configured endpoint.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BaseAddressFromHttpClientMatchesTest()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/someendpoint")
            .WithExactQueryString(string.Empty)
            .Respond("application/json", "Ok");

        var client = new HttpClient(mockHttp) { BaseAddress = new("http://foo") };

        var fixture = RestService.For<ITrimTrailingForwardSlashApi>(client);

        await fixture.Get();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a trailing slash on the HttpClient base address is handled.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BaseAddressWithTrailingSlashFromHttpClientMatchesTest()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/someendpoint")
            .WithExactQueryString(string.Empty)
            .Respond("application/json", "Ok");

        var client = new HttpClient(mockHttp) { BaseAddress = new("http://foo/") };

        var fixture = RestService.For<ITrimTrailingForwardSlashApi>(client);

        await fixture.Get();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a request before fixture creation does not break trailing-slash handling.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BaseAddressWithTrailingSlashCalledBeforeFromHttpClientMatchesTest()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/firstRequest")
            .WithExactQueryString(string.Empty)
            .Respond("application/json", "Ok");
        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/someendpoint")
            .WithExactQueryString(string.Empty)
            .Respond("application/json", "Ok");

        var client = new HttpClient(mockHttp) { BaseAddress = new("http://foo/") };

        _ = await client.GetAsync(new Uri("/firstRequest", UriKind.RelativeOrAbsolute));

        var fixture = RestService.For<ITrimTrailingForwardSlashApi>(client);

        await fixture.Get();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a GET with no parameters works with a trailing slash in the base URL.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithNoParametersTestTrailingSlashInBase()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/someendpoint")
            .WithExactQueryString(string.Empty)
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<ITrimTrailingForwardSlashApi>("http://foo/", settings);

        await fixture.Get();
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a path-bound object maps its properties into the path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundObject()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/foos/1/bar/barNone")
            .WithExactQueryString(string.Empty)
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

        await fixture.GetFooBars(
            new PathBoundObject { SomeProperty = 1, SomeProperty2 = "barNone" });
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a long path-bound value is mapped into the path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithLongPathBoundObject()
    {
        var mockHttp = new MockHttpMessageHandler();
        var longPathString = string.Concat(Enumerable.Repeat("barNone", 1000));
        _ = mockHttp
            .Expect(HttpMethod.Get, $"http://foo/foos/12345/bar/{longPathString}")
            .WithExactQueryString(string.Empty)
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

        await fixture.GetFooBars(
            new PathBoundObject { SomeProperty = 12_345, SomeProperty2 = longPathString });
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies path tokens with different casing still bind to the object's properties.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundObjectDifferentCasing()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/foos/1/bar/barNone")
            .WithExactQueryString(string.Empty)
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

        await fixture.GetFooBarsWithDifferentCasing(
            new() { SomeProperty = 1, SomeProperty2 = "barNone" });
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies an explicit parameter combines with a path-bound object.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundObjectAndParameter()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/foos/myId/22/bar/bart")
            .WithExactQueryString(string.Empty)
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

        await fixture.GetBarsByFoo(
            "myId",
            new() { SomeProperty = 22, SomeProperty2 = "bart" });
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies an explicit parameter takes precedence over a path-bound property.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundObjectAndParameterParameterPrecedence()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/foos/chooseMe/bar/barNone")
            .WithExactQueryString([new("SomeProperty", "1")])
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

        await fixture.GetFooBars(
            new() { SomeProperty = 1, SomeProperty2 = "barNone" },
            "chooseMe");
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a derived path-bound object maps its properties into the path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundDerivedObject()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/foos/1/bar/test")
            .WithExactQueryString(
                [new("SomeProperty2", "barNone")])
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

        await fixture.GetFooBarsDerived(
            new()
            {
                SomeProperty = 1,
                SomeProperty2 = "barNone",
                SomeProperty3 = "test"
            });
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a derived object passed as its base type does not duplicate the bound property.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithDerivedObjectAsBaseType()
    {
        // see https://github.com/reactiveui/refit/issues/1882: a property bound to the
        // path must not also be emitted as a query parameter when a derived instance is
        // passed for a base-typed parameter.
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/foos/1/bar")
            .WithExactQueryString(
                [
                    new("SomeProperty3", "test"),
                    new("SomeProperty2", "barNone")
                ])
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

        await fixture.GetBarsByFoo(
            new PathBoundDerivedObject
            {
                SomeProperty = 1,
                SomeProperty2 = "barNone",
                SomeProperty3 = "test"
            });
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a path-bound object combines with an explicit query parameter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundObjectAndQueryParameter()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/foos/22/bar")
            .WithExactQueryString(
                [new("SomeProperty2", "bart")])
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

        await fixture.GetBarsByFoo(
            new() { SomeProperty = 22, SomeProperty2 = "bart" });
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a POST binds a path object alongside a body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostFooBarPathBoundObject()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Post, "http://foo/foos/22/bar/bart")
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

        await fixture.PostFooBar(
            new() { SomeProperty = 22, SomeProperty2 = "bart" },
            new { });
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies path-bound collection values respect the configured URL formatter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PathBoundObjectsRespectFormatter()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/foos/22%2C23")
            .Respond("application/json", "Ok");

        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHttp,
            UrlParameterFormatter = new TestEnumerableUrlParameterFormatter()
        };
        var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

        await fixture.GetFoos(
            new()
            {
                Values = [22, 23]
            });
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a path-bound object combines with a query property.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundObjectAndQuery()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/foos/1/bar/barNone")
            .WithExactQueryString("SomeQuery=test")
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

        await fixture.GetFooBars(
            new PathBoundObjectWithQuery
            {
                SomeProperty = 1,
                SomeProperty2 = "barNone",
                SomeQuery = "test"
            });
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a path-bound query property uses its custom format.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundObjectAndQueryWithFormat()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/foo")
            .WithExactQueryString("SomeQueryWithFormat=2020-03-05T13:55:00Z")
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

        await fixture.GetBarsWithCustomQueryFormat(
            new()
            {
                SomeQueryWithFormat = new DateTimeOffset(2020, 03, 05, 13, 55, 00, TimeSpan.Zero).UtcDateTime
            });

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a path-bound object combines with a query object body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundObjectAndQueryObject()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Post, "http://foo/foos/1/bar/barNone")
            .WithExactQueryString("Property1=test&Property2=test2")
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

        await fixture.PostFooBar(
            new() { SomeProperty = 1, SomeProperty2 = "barNone" },
            new() { Property1 = "test", Property2 = "test2" });
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a multipart POST binds a path object and uploads a stream part.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostFooBarPathMultipart()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Post, "http://foo/foos/22/bar/bar")
            .WithExactQueryString(string.Empty)
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

        await using var stream = GetTestFileStream("Test Files/Test.pdf");
        await fixture.PostFooBarStreamPart(
            new PathBoundObject { SomeProperty = 22, SomeProperty2 = "bar" },
            new(stream, "Test.pdf", "application/pdf"));
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a multipart POST binds a path-and-query object and uploads a stream part.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostFooBarPathQueryMultipart()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Post, "http://foo/foos/22/bar/bar")
            .WithExactQueryString("SomeQuery=test")
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

        await using var stream = GetTestFileStream("Test Files/Test.pdf");
        await fixture.PostFooBarStreamPart(
            new PathBoundObjectWithQuery
            {
                SomeProperty = 22,
                SomeProperty2 = "bar",
                SomeQuery = "test"
            },
            new(stream, "Test.pdf", "application/pdf"));
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a multipart POST binds a path object, query object, and stream part.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostFooBarPathQueryObjectMultipart()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .Expect(HttpMethod.Post, "http://foo/foos/22/bar/bar")
            .WithExactQueryString("Property1=test&Property2=test2")
            .Respond("application/json", "Ok");

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<IApiBindPathToObject>("http://foo", settings);

        await using var stream = GetTestFileStream("Test Files/Test.pdf");
        await fixture.PostFooBarStreamPart(
            new() { SomeProperty = 22, SomeProperty2 = "bar" },
            new() { Property1 = "test", Property2 = "test2" },
            new(stream, "Test.pdf", "application/pdf"));
        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies content is not automatically added to a GET request.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DoesntAddAutoAddContentToGetRequest()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/nobody")

            // We can't add HttpContent to a GET request,
            // because HttpClient doesn't allow it and it will
            // blow up at runtime
            .With(r => r.Content is null)
            .Respond("application/json", "Ok");

        var fixture = RestService.For<IBodylessApi>("http://foo", settings);

        await fixture.Get();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies content is not automatically added to a HEAD request.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DoesntAddAutoAddContentToHeadRequest()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Head, "http://foo/nobody")

            // We can't add HttpContent to a HEAD request,
            // because HttpClient doesn't allow it and it will
            // blow up at runtime
            .With(r => r.Content is null)
            .Respond("application/json", "Ok");

        var fixture = RestService.For<IBodylessApi>("http://foo", settings);

        await fixture.Head();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a decimal query parameter is formatted correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithDecimal()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/withDecimal")
            .WithExactQueryString([new("value", "3.456")])
            .Respond("application/json", "Ok");

        var fixture = RestService.For<IApiWithDecimal>("http://foo", settings);

        const decimal val = 3.456M;

        _ = await fixture.GetWithDecimal(val);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a decimal query parameter is formatted correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithDecimalGenerated()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/withDecimal")
            .WithExactQueryString([new("value", "3.456")])
            .Respond("application/json", "Ok");

        var fixture = RestService.For<IApiWithDecimal>("http://foo", settings);

        const decimal val = 3.456M;

        _ = await fixture.GetWithDecimalGenerated(val);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a path parameter is formatted correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathParameterGenerated()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/bar")
            .Respond("application/json", "Ok");

        var fixture = RestService.ForGenerated<IGeneratedParametersApi>("http://foo", settings);

        _ = await fixture.GetPath("bar");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a query parameter is formatted correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithQueryParameterGenerated()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/?q=bar")
            .Respond("application/json", "Ok");

        var fixture = RestService.ForGenerated<IGeneratedParametersApi>("http://foo", settings);

        _ = await fixture.GetQuery("bar");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies an aliased query parameter is formatted correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithAliasedQueryParameterGenerated()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/?q=bar")
            .Respond("application/json", "Ok");

        var fixture = RestService.ForGenerated<IGeneratedParametersApi>("http://foo", settings);

        _ = await fixture.GetQueryAlias("bar");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a URL with multiple query parameters is formatted correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithMultipleParametersGenerated()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/1/800x600/foo")
            .Respond("application/json", "Ok");

        var fixture = RestService.ForGenerated<IGeneratedParametersApi>("http://foo", settings);

        _ = await fixture.FetchSomethingWithMultipleParametersPerSegment(1, 800, 600);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a URL with multiple repeated query parameters is formatted correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithMultipleRepeatedParametersGenerated()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/1/300x300/foo")
            .Respond("application/json", "Ok");

        var fixture = RestService.ForGenerated<IGeneratedParametersApi>("http://foo", settings);

        _ = await fixture.FetchSomethingWithMultipleRepeatedParametersPerSegment(1, 300);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a URL with a nullable parameters is formatted correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithNullableParameterGenerated()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Get, "http://foo/a//b")
            .Respond("application/json", "Ok");

        var fixture = RestService.ForGenerated<IGeneratedParametersApi>("http://foo", settings);

        _ = await fixture.GetNullableParam(null);

        mockHttp.VerifyNoOutstandingExpectation();
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
        var fullName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(relativeName, StringComparison.InvariantCulture))
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
