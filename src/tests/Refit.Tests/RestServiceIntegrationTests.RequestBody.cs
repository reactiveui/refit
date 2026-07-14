// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Refit.Testing;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Refit.Tests;

/// <summary>Integration tests that exercise <see cref="RestService"/> end to end against a mock HTTP handler.</summary>
public partial class RestServiceIntegrationTests
{
    /// <summary>A sample value routed through a form field named 'Password' to exercise field-descriptor serialization.</summary>
    private const string SensitiveFormValue = "secret";

    /// <summary>A sample integer form value exercising the reflection-free numeric fast path.</summary>
    private const int SampleFormAge = 42;

    /// <summary>A sample formatted numeric form value; rendered as <c>1.50</c> via <c>[Query(Format = "0.00")]</c>.</summary>
    private const double SampleFormRatio = 1.5;

    /// <summary>Base URL for the httpbin.org request-bin exchanges.</summary>
    private const string HttpBinBaseUrl = "http://httpbin.org/";

    /// <summary>URL for the httpbin.org foo endpoint.</summary>
    private const string HttpBinFooUrl = "http://httpbin.org/foo";

    /// <summary>Sample last-name query value.</summary>
    private const string RamboLastName = "Rambo";

    /// <summary>Sample generic integer body value.</summary>
    private const int GenericIntValue = 5;

    /// <summary>Sample metadata age value.</summary>
    private const int MetaDataAge = 99;

    /// <summary>Sample "Other" collection numeric query value.</summary>
    private const int OtherQueryNumber = 12_345;

    /// <summary>The X-Refit header value passed as the integer type argument in httpbin exchanges.</summary>
    private const int XRefitHeaderValue = 99;

    /// <summary>The sample postal code used in complex query-parameter exchanges.</summary>
    private const int PostcodeValue = 9999;

    /// <summary>The custom header name exchanged with httpbin in the header tests.</summary>
    private const string RefitHeaderName = "X-Refit";

    /// <summary>Stub endpoint URL used by the no-body POST tests.</summary>
    private const string HttpBinPostUrl = "http://httpbin.org/1h3a5jm1";

    /// <summary>The error response body returned by the bad-request stub.</summary>
    private const string BadErrorJson = "{\"error\":\"bad\"}";

    /// <summary>Verifies the npmjs registry can be queried.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HitTheNpmJs()
    {
        var handler = new StubHttp
        {
            {
                Route.Get("https://registry.npmjs.org/congruence"),
                Reply.Json("{ \"_id\":\"congruence\", \"_rev\":\"rev\" , \"name\":\"name\"}")
            },
        };

        var fixture = handler.CreateClient<INpmJs>("https://registry.npmjs.org");
        var result = await fixture.GetCongruence();

        await Assert.That(result._id).IsEqualTo("congruence");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a POST with no body works.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostToRequestBin()
    {
        var handler = new StubHttp
        {
            {
                Route.Post(HttpBinPostUrl),
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IRequestBin>(HttpBinBaseUrl);

        await fixture.Post();

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies posting a raw string body using the default serialization method works.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostStringDefaultToRequestBin()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Post, Template = HttpBinFooUrl, Body = "raw string" },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IRequestBin>(HttpBinBaseUrl);

        await fixture.PostRawStringDefault("raw string");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a body parameter named like a generated local variable still works (issue #2161).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostBodyNamedLikeGeneratedLocalToRequestBin()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Post, Template = HttpBinFooUrl, Body = "payload" },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IRequestBin>(HttpBinBaseUrl);

        await fixture.PostBodyNamedLikeGeneratedLocal("payload");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a collection body is sent as JSON Lines (newline-delimited JSON).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostJsonLinesToRequestBin()
    {
        const string expected = "{\"id\":\"124\",\"name\":\"Stark Industries\"}\n{\"id\":\"125\",\"name\":\"Acme Corp\"}";

        var handler = new StubHttp
        {
            {
                new RouteMatcher
                {
                    Method = HttpMethod.Post,
                    Template = HttpBinFooUrl,
                    Body = expected,
                    Where = static message => message.Content!.Headers.ContentType!.MediaType == "application/x-ndjson",
                },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IRequestBin>(HttpBinBaseUrl);

        await fixture.PostJsonLines(
            [
                new() { Id = "124", Name = "Stark Industries" },
                new() { Id = "125", Name = "Acme Corp" }
            ]);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies posting a raw string body serialized as JSON works.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostStringJsonToRequestBin()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Post, Template = HttpBinFooUrl, Headers = [("Content-Type", "application/json; charset=utf-8")], Body = "\"json string\"" },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IRequestBin>(HttpBinBaseUrl);

        await fixture.PostRawStringJson("json string");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies posting a raw string body url-encoded works.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostStringUrlToRequestBin()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Post, Template = HttpBinFooUrl, Headers = [("Content-Type", "application/x-www-form-urlencoded; charset=utf-8")], Body = "url%26string" },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IRequestBin>(HttpBinBaseUrl);

        await fixture.PostRawStringUrlEncoded("url&string");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies posting a generic body works for multiple types.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostToRequestBinWithGenerics()
    {
        var handler = new StubHttp
        {
            {
                Route.Post(HttpBinPostUrl),
                Reply.Status(HttpStatusCode.OK)
            },
            {
                Route.Post(HttpBinPostUrl),
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IRequestBin>(HttpBinBaseUrl);

        await fixture.PostGeneric(GenericIntValue);

        await fixture.PostGeneric("4");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a buffered void-returning body sets the content length header.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostWithVoidReturnBufferedBodyExpectContentLengthHeader()
    {
        var postBody = new Dictionary<string, string> { { "some", "body" }, { "once", "told me" } };

        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Post, Template = HttpBinFooUrl, Where = static request => request.Content?.Headers.ContentLength > 0 },
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IRequestBin>(HttpBinBaseUrl);

        await fixture.PostVoidReturnBodyBuffered(postBody);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a buffered non-void-returning body sets the content length header.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostWithNonVoidReturnBufferedBodyExpectContentLengthHeader()
    {
        var postBody = new Dictionary<string, string> { { "some", "body" }, { "once", "told me" } };
        const string expectedResponse = "some response";

        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Post, Template = HttpBinFooUrl, Where = static request => request.Content?.Headers.ContentLength > 0 },
                Reply.Text(expectedResponse, "text/plain")
            },
        };

        var fixture = handler.CreateClient<IRequestBin>(HttpBinBaseUrl);

        var result = await fixture.PostNonVoidReturnBodyBuffered(postBody);

        await handler.VerifyAllCalledAsync();

        await Assert.That(result).IsEqualTo(expectedResponse);
    }

    /// <summary>Verifies a method whose parameter name matches the code-gen variable works.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UseMethodWithArgumentsParameter()
    {
        var handler = new StubHttp
        {
            {
                Route.Get("http://httpbin.org/foo/something"),
                Reply.Status(HttpStatusCode.OK)
            },
        };

        var fixture = handler.CreateClient<IRequestBin>(HttpBinBaseUrl);

        await fixture.SomeApiThatUsesVariableNameFromCodeGen("something");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies large data can be serialized and posted.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CanSerializeBigData()
    {
        const int bigDataByteCount = 800_000;
        const int byteModulus = 256;
        var bigObject = new BigObject
        {
            BigData = [.. Enumerable.Range(0, bigDataByteCount).Select(static x => (byte)(x % byteModulus))]
        };

        var handler = new StubHttp
        {
            {
                new RouteMatcher
                {
                    Method = HttpMethod.Post,
                    Template = "http://httpbin.org/big",
                    WhereAsync = async m =>
                    {
                        await using var s = await m.Content!.ReadAsStreamAsync();
                        var it = await JsonSerializer.DeserializeAsync<BigObject>(s, _camelCaseJsonOptions);
                        return it!.BigData!.SequenceEqual(bigObject.BigData!);
                    },
                },
                Reply.Status(HttpStatusCode.OK)
            },
};

        var fixture = handler.CreateClient<IRequestBin>(HttpBinBaseUrl, new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer()
        });

        await fixture.PostBig(bigObject);

        await handler.VerifyAllCalledAsync();
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
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "http://httpbin.org/get", Query = [("param", "foo")], Headers = [(RefitHeaderName, "99")] },
                Reply.Json("{\"url\": \"http://httpbin.org/get?param=foo\", \"args\": {\"param\": \"foo\"}, \"headers\":{\"X-Refit\":\"99\"}}")
            },
        };

        var fixture = handler.CreateClient<IHttpBinApi<HttpBinGet, string, int>>("http://httpbin.org/get");

        var result = await fixture.Get("foo", XRefitHeaderValue);

        await Assert.That(result.Url).IsEqualTo("http://httpbin.org/get?param=foo");
        await Assert.That(result.Args!["param"]).IsEqualTo("foo");
        await Assert.That(result.Headers![RefitHeaderName]).IsEqualTo("99");

        await handler.VerifyAllCalledAsync();
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
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = HttpBinGetUrl, Headers = [(RefitHeaderName, "99")] },
                Reply.Json("{\"url\": \"https://httpbin.org/get?FirstName=John&LastName=Rambo\", \"args\": {\"FirstName\": \"John\", \"lName\": \"Rambo\"}}")
            },
        };

        var settings = handler.ToSettings();

        var myParams = new MySimpleQueryParams { FirstName = "John", LastName = RamboLastName };

        var fixture = RestService.For<IHttpBinApi<HttpBinGet, MySimpleQueryParams, int>>(
            HttpBinGetUrl,
            settings);

        var resp = await fixture.Get(myParams, XRefitHeaderValue);

        await Assert.That(resp.Args![FirstNameKey]).IsEqualTo("John");
        await Assert.That(resp.Args["lName"]).IsEqualTo(RamboLastName);
    }

    /// <summary>Verifies complex dynamic query parameters are echoed correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ComplexDynamicQueryparametersTest()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(HttpBinGetUrl),
                Reply.Json("{\"url\": \"https://httpbin.org/get?hardcoded=true&FirstName=John&LastName=Rambo"
                    + "&Addr_Zip=9999&Addr_Street=HomeStreet 99&MetaData_Age=99&MetaData_Initials=JR"
                    + "&MetaData_Birthday=10%2F31%2F1918 4%3A21%3A16 PM&Other=12345"
                    + "&Other=10%2F31%2F2017 4%3A21%3A17 PM&Other=696e8653-6671-4484-a65f-9485af95fd3a\", "
                    + "\"args\": { \"Addr_Street\": \"HomeStreet 99\", \"Addr_Zip\": \"9999\", "
                    + "\"FirstName\": \"John\", \"LastName\": \"Rambo\", \"MetaData_Age\": \"99\", "
                    + "\"MetaData_Birthday\": \"10/31/1981 4:32:59 PM\", \"MetaData_Initials\": \"JR\", "
                    + "\"Other\": [\"12345\",\"10/31/2017 4:32:59 PM\",\"60282dd2-f79a-4400-be01-bcb0e86e7bc6\"], "
                    + "\"hardcoded\": \"true\"}}")
            },
        };

        var settings = handler.ToSettings();

        var myParams = new MyComplexQueryParams
        {
            FirstName = "John",
            LastName = RamboLastName,
            Address = new() { Postcode = PostcodeValue, Street = "HomeStreet 99" },
        };

        myParams.MetaData.Add("Age", MetaDataAge);
        myParams.MetaData.Add("Initials", "JR");
        myParams.MetaData.Add("Birthday", new DateTimeOffset(1981, 10, 31, 16, 24, 59, TimeSpan.Zero).UtcDateTime);

        myParams.Other.Add(OtherQueryNumber);
        myParams.Other.Add(new DateTimeOffset(2017, 10, 31, 16, 24, 59, TimeSpan.Zero).UtcDateTime);
        myParams.Other.Add(new Guid("60282dd2-f79a-4400-be01-bcb0e86e7bc6"));

        var fixture = RestService.For<IHttpBinApi<HttpBinGet, MyComplexQueryParams, int>>(
            "https://httpbin.org",
            settings);

        var resp = await fixture.GetQuery(myParams);

        await Assert.That(resp.Args![FirstNameKey]).IsEqualTo("John");
        await Assert.That(resp.Args["LastName"]).IsEqualTo(RamboLastName);
        await Assert.That(resp.Args["Addr_Zip"]).IsEqualTo("9999");
    }

    /// <summary>Verifies complex dynamic query parameters work for POST requests.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ComplexPostDynamicQueryparametersTest()
    {
        var handler = new StubHttp
        {
            {
                Route.Post("https://httpbin.org/post"),
                Reply.Json("{\"url\": \"https://httpbin.org/post?hardcoded=true&FirstName=John&LastName=Rambo"
                    + "&Addr_Zip=9999&Addr_Street=HomeStreet 99&MetaData_Age=99&MetaData_Initials=JR"
                    + "&MetaData_Birthday=10%2F31%2F1918 4%3A21%3A16 PM&Other=12345"
                    + "&Other=10%2F31%2F2017 4%3A21%3A17 PM&Other=696e8653-6671-4484-a65f-9485af95fd3a\", "
                    + "\"args\": { \"Addr_Street\": \"HomeStreet 99\", \"Addr_Zip\": \"9999\", "
                    + "\"FirstName\": \"John\", \"LastName\": \"Rambo\", \"MetaData_Age\": \"99\", "
                    + "\"MetaData_Birthday\": \"10/31/1981 4:32:59 PM\", \"MetaData_Initials\": \"JR\", "
                    + "\"Other\": [\"12345\",\"10/31/2017 4:32:59 PM\",\"60282dd2-f79a-4400-be01-bcb0e86e7bc6\"], "
                    + "\"hardcoded\": \"true\"}}")
            },
        };

        var settings = handler.ToSettings();

        var myParams = new MyComplexQueryParams
        {
            FirstName = "John",
            LastName = RamboLastName,
            Address = new() { Postcode = PostcodeValue, Street = "HomeStreet 99" },
        };

        myParams.MetaData.Add("Age", MetaDataAge);
        myParams.MetaData.Add("Initials", "JR");
        myParams.MetaData.Add("Birthday", new DateTimeOffset(1981, 10, 31, 16, 24, 59, TimeSpan.Zero).UtcDateTime);

        myParams.Other.Add(OtherQueryNumber);
        myParams.Other.Add(new DateTimeOffset(2017, 10, 31, 16, 24, 59, TimeSpan.Zero).UtcDateTime);
        myParams.Other.Add(new Guid("60282dd2-f79a-4400-be01-bcb0e86e7bc6"));

        var fixture = RestService.For<IHttpBinApi<HttpBinGet, MyComplexQueryParams, int>>(
            "https://httpbin.org",
            settings);

        var resp = await fixture.PostQuery(myParams);

        await Assert.That(resp.Args![FirstNameKey]).IsEqualTo("John");
        await Assert.That(resp.Args["LastName"]).IsEqualTo(RamboLastName);
        await Assert.That(resp.Args["Addr_Zip"]).IsEqualTo("9999");
    }

    /// <summary>Verifies an object URL-encoded body flows through generated reflection-free form fields.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostGeneratedUrlEncodedFormUsesFieldDescriptors()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher
                {
                    Method = HttpMethod.Post,
                    Template = "http://foo/form",
                    FormData =
                    [
                        ("user_name", "bob"),
                        ("pwd", "secret"),
                        ("Plain", "x"),
                        ("Nullable", string.Empty),
                        ("Age", "42"),
                        ("Color", "Green"),
                        ("Ratio", "1.50"),
                        ("addr-City", "NYC"),
                    ],
                },
                Reply.Json("\"ok\"")
            },
};

        var fixture = handler.CreateClient<IGeneratedFormApi>("http://foo");

        _ = await fixture.PostForm(
            new GeneratedFormData
            {
                UserName = "bob",
                Password = SensitiveFormValue,
                Plain = "x",
                Nullable = null,
                Age = SampleFormAge,
                Color = GeneratedFormColor.Green,
                Ratio = SampleFormRatio,
                City = "NYC",
            });

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies opt-in request content capture exposes the sent body on the thrown ApiException (#1189).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CaptureRequestContentExposesSentBodyOnApiException()
    {
        var handler = new StubHttp
        {
            {
                Route.Post(HttpBinFooUrl),
                Reply.Json(BadErrorJson, HttpStatusCode.BadRequest)
            },
        };

        var fixture = handler.CreateClient<IRequestBin>(HttpBinBaseUrl, new RefitSettings
        {
            CaptureRequestContent = true,
        });

        var exception = await Assert
            .That(() => fixture.PostRawStringJson("hello"))
            .ThrowsExactly<ApiException>();

        await Assert.That(exception!.HasRequestContent).IsTrue();
        await Assert.That(exception.RequestContent).IsEqualTo("\"hello\"");
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies request content capture also works on the non-void response path (#1189).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CaptureRequestContentExposesSentBodyOnNonVoidResponse()
    {
        var handler = new StubHttp
        {
            {
                Route.Post(HttpBinFooUrl),
                Reply.Json(BadErrorJson, HttpStatusCode.BadRequest)
            },
        };

        var fixture = handler.CreateClient<IRequestBin>(HttpBinBaseUrl, new RefitSettings
        {
            CaptureRequestContent = true,
        });

        var exception = await Assert
            .That(() => (Task)fixture.PostNonVoidReturnBodyBuffered(new { name = "bob" }))
            .ThrowsExactly<ApiException>();

        await Assert.That(exception!.HasRequestContent).IsTrue();
        await Assert.That(exception.RequestContent).Contains("bob");
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies request content is not captured when the opt-in setting is left disabled (#1189).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RequestContentNotCapturedByDefault()
    {
        var handler = new StubHttp
        {
            {
                Route.Post(HttpBinFooUrl),
                Reply.Json(BadErrorJson, HttpStatusCode.BadRequest)
            },
        };

        var fixture = handler.CreateClient<IRequestBin>(HttpBinBaseUrl);

        var exception = await Assert
            .That(() => fixture.PostRawStringJson("hello"))
            .ThrowsExactly<ApiException>();

        await Assert.That(exception!.HasRequestContent).IsFalse();
        await Assert.That(exception.RequestContent).IsNull();
        await handler.VerifyAllCalledAsync();
    }
}
