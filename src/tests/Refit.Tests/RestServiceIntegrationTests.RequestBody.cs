// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using RichardSzalay.MockHttp;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Refit.Tests;

/// <summary>Integration tests that exercise <see cref="RestService"/> end to end against a mock HTTP handler.</summary>
public partial class RestServiceIntegrationTests
{
    /// <summary>Verifies the npmjs registry can be queried.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheNpmJs()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://registry.npmjs.org/congruence")
            .Respond(
                "application/json",
                "{ \"_id\":\"congruence\", \"_rev\":\"rev\" , \"name\":\"name\"}");

        var fixture = RestService.For<INpmJs>("https://registry.npmjs.org", settings);
        var result = await fixture.GetCongruence();

        await Assert.That(result._id).IsEqualTo("congruence");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a POST with no body works.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostToRequestBin()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp.Expect(HttpMethod.Post, "http://httpbin.org/1h3a5jm1").Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);

        await fixture.Post();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies posting a raw string body using the default serialization method works.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostStringDefaultToRequestBin()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Post, "http://httpbin.org/foo")
            .WithContent("raw string")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);

        await fixture.PostRawStringDefault("raw string");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a collection body is sent as JSON Lines (newline-delimited JSON).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostJsonLinesToRequestBin()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        const string expected = "{\"id\":\"124\",\"name\":\"Stark Industries\"}\n{\"id\":\"125\",\"name\":\"Acme Corp\"}";

        _ = mockHttp
            .Expect(HttpMethod.Post, "http://httpbin.org/foo")
            .With(message => message.Content!.Headers.ContentType!.MediaType == "application/x-ndjson")
            .WithContent(expected)
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);

        await fixture.PostJsonLines(
            [
                new JsonLineRecord { Id = "124", Name = "Stark Industries" },
                new JsonLineRecord { Id = "125", Name = "Acme Corp" }
            ]);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies posting a raw string body serialized as JSON works.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostStringJsonToRequestBin()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Post, "http://httpbin.org/foo")
            .WithContent("\"json string\"")
            .WithHeaders("Content-Type", "application/json; charset=utf-8")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);

        await fixture.PostRawStringJson("json string");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies posting a raw string body url-encoded works.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostStringUrlToRequestBin()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Post, "http://httpbin.org/foo")
            .WithContent("url%26string")
            .WithHeaders("Content-Type", "application/x-www-form-urlencoded; charset=utf-8")
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);

        await fixture.PostRawStringUrlEncoded("url&string");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies posting a generic body works for multiple types.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostToRequestBinWithGenerics()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp.Expect(HttpMethod.Post, "http://httpbin.org/1h3a5jm1").Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);

        await fixture.PostGeneric(5);

        mockHttp.VerifyNoOutstandingExpectation();

        mockHttp.ResetExpectations();

        _ = mockHttp.Expect(HttpMethod.Post, "http://httpbin.org/1h3a5jm1").Respond(HttpStatusCode.OK);

        await fixture.PostGeneric("4");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a buffered void-returning body sets the content length header.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostWithVoidReturnBufferedBodyExpectContentLengthHeader()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        var postBody = new Dictionary<string, string> { { "some", "body" }, { "once", "told me" } };

        _ = mockHttp
            .Expect(HttpMethod.Post, "http://httpbin.org/foo")
            .With(request => request.Content?.Headers.ContentLength > 0)
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);

        await fixture.PostVoidReturnBodyBuffered(postBody);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies a buffered non-void-returning body sets the content length header.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostWithNonVoidReturnBufferedBodyExpectContentLengthHeader()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        var postBody = new Dictionary<string, string> { { "some", "body" }, { "once", "told me" } };
        const string expectedResponse = "some response";

        _ = mockHttp
            .Expect(HttpMethod.Post, "http://httpbin.org/foo")
            .With(request => request.Content?.Headers.ContentLength > 0)
            .Respond("text/plain", expectedResponse);

        var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);

        var result = await fixture.PostNonVoidReturnBodyBuffered(postBody);

        mockHttp.VerifyNoOutstandingExpectation();

        await Assert.That(result).IsEqualTo(expectedResponse);
    }

    /// <summary>Verifies a method whose parameter name matches the code-gen variable works.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UseMethodWithArgumentsParameter()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);

        _ = mockHttp
            .Expect(HttpMethod.Get, "http://httpbin.org/foo/something")
            .Respond(HttpStatusCode.OK);

        await fixture.SomeApiThatUsesVariableNameFromCodeGen("something");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies large data can be serialized and posted.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CanSerializeBigData()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHttp,
            ContentSerializer = new SystemTextJsonContentSerializer()
        };

        var bigObject = new BigObject
        {
            BigData = [.. Enumerable.Range(0, 800_000).Select(x => (byte)(x % 256))]
        };

        _ = mockHttp
            .Expect(HttpMethod.Post, "http://httpbin.org/big")
            .With(m =>
            {
                async Task<bool> CompareBodyAsync()
                {
                    await using var s = await m.Content!.ReadAsStreamAsync();
                    var it = await JsonSerializer.DeserializeAsync<BigObject>(s, _camelCaseJsonOptions);
                    return it!.BigData!.SequenceEqual(bigObject.BigData!);
                }

                return CompareBodyAsync().Result;
            })
            .Respond(HttpStatusCode.OK);

        var fixture = RestService.For<IRequestBin>("http://httpbin.org/", settings);

        await fixture.PostBig(bigObject);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies non-Refit interfaces throw a meaningful exception.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NonRefitInterfacesThrowMeaningfulExceptions()
    {
        try
        {
            _ = RestService.For<INoRefitHereBuddy>("http://example.com");
        }
        catch (InvalidOperationException exception)
        {
            await Assert.That(exception.Message).StartsWith("INoRefitHereBuddy", StringComparison.Ordinal);
            await Assert.That(exception.Message).Contains("Refit source generator", StringComparison.Ordinal);
            await Assert.That(exception.Message).Contains("Native AOT", StringComparison.Ordinal);
        }
    }

    /// <summary>Verifies non-Refit methods throw a meaningful exception.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NonRefitMethodsThrowMeaningfulExceptions()
    {
        try
        {
            var fixture = RestService.For<IAmHalfRefit>("http://example.com");
            await fixture.Get();
        }
        catch (NotImplementedException exception)
        {
            await Assert.That(exception.Message).Contains("no Refit HTTP method attribute");
        }
    }

    /// <summary>Verifies generic interface parameters work end to end.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GenericsWork()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Get, "http://httpbin.org/get")
            .WithHeaders("X-Refit", "99")
            .WithQueryString("param", "foo")
            .Respond(
                "application/json",
                "{\"url\": \"http://httpbin.org/get?param=foo\", \"args\": {\"param\": \"foo\"}, \"headers\":{\"X-Refit\":\"99\"}}");

        var fixture = RestService.For<IHttpBinApi<HttpBinGet, string, int>>(
            "http://httpbin.org/get",
            settings);

        var result = await fixture.Get("foo", 99);

        await Assert.That(result.Url).IsEqualTo("http://httpbin.org/get?param=foo");
        await Assert.That(result.Args!["param"]).IsEqualTo("foo");
        await Assert.That(result.Headers!["X-Refit"]).IsEqualTo("99");

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies value-type responses work even though they are not strictly valid.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ValueTypesArentValidButTheyWorkAnyway()
    {
        var handler = new TestHttpMessageHandler("true");

        var fixture = RestService.For<IBrokenWebApi>(
            new HttpClient(handler) { BaseAddress = new("http://nowhere.com") });

        var result = await fixture.PostAValue("Does this work?");

        await Assert.That(result).IsTrue();
    }

    /// <summary>Verifies a missing base URL throws an exception when a method is invoked.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MissingBaseUrlThrowsArgumentException()
    {
        var client = new HttpClient(); // No BaseUrl specified

        var fixture = RestService.For<IGitHubApi>(client);

        // We should get an InvalidOperationException if we call a method without a base address set
        var result = await Assert.That(
            () => (Task)fixture.GetUser(null!)).ThrowsExactly<InvalidOperationException>();

        await AssertStackTraceContains(nameof(IGitHubApi.GetUser), result!.StackTrace);
    }

    /// <summary>Verifies simple dynamic query parameters are echoed correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SimpleDynamicQueryparametersTest()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .WithHeaders("X-Refit", "99")
            .Respond(
                "application/json",
                "{\"url\": \"https://httpbin.org/get?FirstName=John&LastName=Rambo\", \"args\": {\"FirstName\": \"John\", \"lName\": \"Rambo\"}}");

        var myParams = new MySimpleQueryParams { FirstName = "John", LastName = "Rambo" };

        var fixture = RestService.For<IHttpBinApi<HttpBinGet, MySimpleQueryParams, int>>(
            "https://httpbin.org/get",
            settings);

        var resp = await fixture.Get(myParams, 99);

        await Assert.That(resp.Args!["FirstName"]).IsEqualTo("John");
        await Assert.That(resp.Args["lName"]).IsEqualTo("Rambo");
    }

    /// <summary>Verifies complex dynamic query parameters are echoed correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ComplexDynamicQueryparametersTest()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Get, "https://httpbin.org/get")
            .Respond(
                "application/json",
                "{\"url\": \"https://httpbin.org/get?hardcoded=true&FirstName=John&LastName=Rambo"
                    + "&Addr_Zip=9999&Addr_Street=HomeStreet 99&MetaData_Age=99&MetaData_Initials=JR"
                    + "&MetaData_Birthday=10%2F31%2F1918 4%3A21%3A16 PM&Other=12345"
                    + "&Other=10%2F31%2F2017 4%3A21%3A17 PM&Other=696e8653-6671-4484-a65f-9485af95fd3a\", "
                    + "\"args\": { \"Addr_Street\": \"HomeStreet 99\", \"Addr_Zip\": \"9999\", "
                    + "\"FirstName\": \"John\", \"LastName\": \"Rambo\", \"MetaData_Age\": \"99\", "
                    + "\"MetaData_Birthday\": \"10/31/1981 4:32:59 PM\", \"MetaData_Initials\": \"JR\", "
                    + "\"Other\": [\"12345\",\"10/31/2017 4:32:59 PM\",\"60282dd2-f79a-4400-be01-bcb0e86e7bc6\"], "
                    + "\"hardcoded\": \"true\"}}");

        var myParams = new MyComplexQueryParams
        {
            FirstName = "John",
            LastName = "Rambo",
            Address = new() { Postcode = 9999, Street = "HomeStreet 99" },
        };

        myParams.MetaData.Add("Age", 99);
        myParams.MetaData.Add("Initials", "JR");
        myParams.MetaData.Add("Birthday", new DateTimeOffset(1981, 10, 31, 16, 24, 59, TimeSpan.Zero).UtcDateTime);

        myParams.Other.Add(12_345);
        myParams.Other.Add(new DateTimeOffset(2017, 10, 31, 16, 24, 59, TimeSpan.Zero).UtcDateTime);
        myParams.Other.Add(new Guid("60282dd2-f79a-4400-be01-bcb0e86e7bc6"));

        var fixture = RestService.For<IHttpBinApi<HttpBinGet, MyComplexQueryParams, int>>(
            "https://httpbin.org",
            settings);

        var resp = await fixture.GetQuery(myParams);

        await Assert.That(resp.Args!["FirstName"]).IsEqualTo("John");
        await Assert.That(resp.Args["LastName"]).IsEqualTo("Rambo");
        await Assert.That(resp.Args["Addr_Zip"]).IsEqualTo("9999");
    }

    /// <summary>Verifies complex dynamic query parameters work for POST requests.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ComplexPostDynamicQueryparametersTest()
    {
        var mockHttp = new MockHttpMessageHandler();

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        _ = mockHttp
            .Expect(HttpMethod.Post, "https://httpbin.org/post")
            .Respond(
                "application/json",
                "{\"url\": \"https://httpbin.org/post?hardcoded=true&FirstName=John&LastName=Rambo"
                    + "&Addr_Zip=9999&Addr_Street=HomeStreet 99&MetaData_Age=99&MetaData_Initials=JR"
                    + "&MetaData_Birthday=10%2F31%2F1918 4%3A21%3A16 PM&Other=12345"
                    + "&Other=10%2F31%2F2017 4%3A21%3A17 PM&Other=696e8653-6671-4484-a65f-9485af95fd3a\", "
                    + "\"args\": { \"Addr_Street\": \"HomeStreet 99\", \"Addr_Zip\": \"9999\", "
                    + "\"FirstName\": \"John\", \"LastName\": \"Rambo\", \"MetaData_Age\": \"99\", "
                    + "\"MetaData_Birthday\": \"10/31/1981 4:32:59 PM\", \"MetaData_Initials\": \"JR\", "
                    + "\"Other\": [\"12345\",\"10/31/2017 4:32:59 PM\",\"60282dd2-f79a-4400-be01-bcb0e86e7bc6\"], "
                    + "\"hardcoded\": \"true\"}}");

        var myParams = new MyComplexQueryParams
        {
            FirstName = "John",
            LastName = "Rambo",
            Address = new() { Postcode = 9999, Street = "HomeStreet 99" },
        };

        myParams.MetaData.Add("Age", 99);
        myParams.MetaData.Add("Initials", "JR");
        myParams.MetaData.Add("Birthday", new DateTimeOffset(1981, 10, 31, 16, 24, 59, TimeSpan.Zero).UtcDateTime);

        myParams.Other.Add(12_345);
        myParams.Other.Add(new DateTimeOffset(2017, 10, 31, 16, 24, 59, TimeSpan.Zero).UtcDateTime);
        myParams.Other.Add(new Guid("60282dd2-f79a-4400-be01-bcb0e86e7bc6"));

        var fixture = RestService.For<IHttpBinApi<HttpBinGet, MyComplexQueryParams, int>>(
            "https://httpbin.org",
            settings);

        var resp = await fixture.PostQuery(myParams);

        await Assert.That(resp.Args!["FirstName"]).IsEqualTo("John");
        await Assert.That(resp.Args["LastName"]).IsEqualTo("Rambo");
        await Assert.That(resp.Args["Addr_Zip"]).IsEqualTo("9999");
    }
}
