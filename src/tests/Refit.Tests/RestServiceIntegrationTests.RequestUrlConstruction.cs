// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Integration tests covering how path-bound objects and formatted parameters are mapped into the request URL.</summary>
public partial class RestServiceIntegrationTests
{
    /// <summary>Verifies a path-bound object maps its properties into the path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundObject()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = FoosBarBarNoneUrl, ExactQuery = string.Empty },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await fixture.GetFooBars(
            new PathBoundObject { SomeProperty = 1, SomeProperty2 = BarNoneValue });
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a generic path-bound parameter binds dotted <c>{obj.Prop}</c> placeholders against the
    /// runtime type instead of throwing at client creation (issue #1743).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithGenericPathBoundObject()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = FoosBarBarNoneUrl, ExactQuery = string.Empty },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await fixture.GetFooBarsGeneric(
            new PathBoundObject { SomeProperty = 1, SomeProperty2 = BarNoneValue });
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a constrained generic path-bound parameter binds dotted <c>{obj.Prop}</c> placeholders against
    /// the constraint type, producing the same URL as the reflection path while being generated inline (issue #2218).</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithGenericConstrainedPathBoundObject()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = FoosBarBarNoneUrl, ExactQuery = string.Empty },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await fixture.GetFooBarsGenericConstrained(
            new PathBoundObject { SomeProperty = 1, SomeProperty2 = BarNoneValue });
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a long path-bound value is mapped into the path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithLongPathBoundObject()
    {
        const int pathRepeatCount = 1000;
        var longPathString = string.Concat(Enumerable.Repeat(BarNoneValue, pathRepeatCount));
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = $"http://foo/foos/12345/bar/{longPathString}", ExactQuery = string.Empty },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await fixture.GetFooBars(
            new PathBoundObject { SomeProperty = LargeFooId, SomeProperty2 = longPathString });
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies path tokens with different casing still bind to the object's properties.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundObjectDifferentCasing()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = FoosBarBarNoneUrl, ExactQuery = string.Empty },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await fixture.GetFooBarsWithDifferentCasing(
            new() { SomeProperty = 1, SomeProperty2 = BarNoneValue });
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies an explicit parameter combines with a path-bound object.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundObjectAndParameter()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "http://foo/foos/myId/22/bar/bart", ExactQuery = string.Empty },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await fixture.GetBarsByFoo(
            "myId",
            new() { SomeProperty = FooId, SomeProperty2 = "bart" });
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies an explicit parameter takes precedence over a path-bound property.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundObjectAndParameterParameterPrecedence()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "http://foo/foos/chooseMe/bar/barNone", ExactQueryParams = [("SomeProperty", "1")] },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await fixture.GetFooBars(
            new() { SomeProperty = 1, SomeProperty2 = BarNoneValue },
            "chooseMe");
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a derived path-bound object maps its properties into the path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundDerivedObject()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "http://foo/foos/1/bar/test", ExactQueryParams = [(SomeProperty2Key, BarNoneValue)] },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await fixture.GetFooBarsDerived(
            new()
            {
                SomeProperty = 1,
                SomeProperty2 = BarNoneValue,
                SomeProperty3 = "test"
            });
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a derived object passed as its base type does not duplicate the bound property.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithDerivedObjectAsBaseType()
    {
        // see https://github.com/reactiveui/refit/issues/1882: a property bound to the path must not also be emitted
        // as a query parameter. SomeProperty binds to the path and so is excluded from the query. The generated
        // request builder flattens the declared (base) type's properties, so SomeProperty2 flattens into the query
        // while the derived-only SomeProperty3 does not contribute through a base-typed parameter — mirroring how the
        // System.Text.Json source generator treats a declared type.
        var handler = new StubHttp
        {
            {
                new RouteMatcher
                {
                    Method = HttpMethod.Get,
                    Template = "http://foo/foos/1/bar",
                    ExactQueryParams = [(SomeProperty2Key, BarNoneValue)],
                },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await fixture.GetBarsByFoo(
            new PathBoundDerivedObject
            {
                SomeProperty = 1,
                SomeProperty2 = BarNoneValue,
                SomeProperty3 = "test"
            });
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a path-bound object combines with an explicit query parameter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundObjectAndQueryParameter()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "http://foo/foos/22/bar", ExactQueryParams = [(SomeProperty2Key, "bart")] },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await fixture.GetBarsByFoo(
            new() { SomeProperty = FooId, SomeProperty2 = "bart" });
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a POST binds a path object alongside a body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostFooBarPathBoundObject()
    {
        var handler = new StubHttp
        {
            {
                Route.Post("http://foo/foos/22/bar/bart"),
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await fixture.PostFooBar(
            new() { SomeProperty = FooId, SomeProperty2 = "bart" },
            new { });
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies path-bound collection values respect the configured URL formatter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PathBoundObjectsRespectFormatter()
    {
        var handler = new StubHttp
        {
            {
                Route.Get("http://foo/foos/22%2C23"),
                Reply.Json("Ok")
            },
        };

        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
            UrlParameterFormatter = new TestEnumerableUrlParameterFormatter()
        };
        var fixture = RestService.For<IApiBindPathToObject>(BaseUrl, settings);

        await fixture.GetFoos(
            new()
            {
                Values = [FooId, SecondFooValue]
            });
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a path-bound object combines with a query property.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundObjectAndQuery()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = FoosBarBarNoneUrl, ExactQuery = "SomeQuery=test" },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await fixture.GetFooBars(
            new PathBoundObjectWithQuery
            {
                SomeProperty = 1,
                SomeProperty2 = BarNoneValue,
                SomeQuery = "test"
            });
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a path-bound query property uses its custom format.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundObjectAndQueryWithFormat()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "http://foo/foo", ExactQuery = "SomeQueryWithFormat=2020-03-05T13:55:00Z" },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await fixture.GetBarsWithCustomQueryFormat(
            new()
            {
                SomeQueryWithFormat = new DateTimeOffset(2020, 03, 05, 13, 55, 00, TimeSpan.Zero).UtcDateTime
            });

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a path-bound object combines with a query object body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathBoundObjectAndQueryObject()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Post, Template = FoosBarBarNoneUrl, ExactQuery = "Property1=test&Property2=test2" },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await fixture.PostFooBar(
            new() { SomeProperty = 1, SomeProperty2 = BarNoneValue },
            new() { Property1 = "test", Property2 = "test2" });
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a multipart POST binds a path object and uploads a stream part.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostFooBarPathMultipart()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Post, Template = Foos22BarBarUrl, ExactQuery = string.Empty },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await using var stream = GetTestFileStream(TestFilePath);
        await fixture.PostFooBarStreamPart(
            new PathBoundObject { SomeProperty = FooId, SomeProperty2 = "bar" },
            new(stream, TestFileName, PdfMediaType));
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a multipart POST binds a path-and-query object and uploads a stream part.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostFooBarPathQueryMultipart()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Post, Template = Foos22BarBarUrl, ExactQuery = "SomeQuery=test" },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await using var stream = GetTestFileStream(TestFilePath);
        await fixture.PostFooBarStreamPart(
            new PathBoundObjectWithQuery
            {
                SomeProperty = FooId,
                SomeProperty2 = "bar",
                SomeQuery = "test"
            },
            new(stream, TestFileName, PdfMediaType));
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a multipart POST binds a path object, query object, and stream part.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PostFooBarPathQueryObjectMultipart()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Post, Template = Foos22BarBarUrl, ExactQuery = "Property1=test&Property2=test2" },
                Reply.Json("Ok")
            },
        };

        var fixture = handler.CreateClient<IApiBindPathToObject>(BaseUrl);

        await using var stream = GetTestFileStream(TestFilePath);
        await fixture.PostFooBarStreamPart(
            new() { SomeProperty = FooId, SomeProperty2 = "bar" },
            new() { Property1 = "test", Property2 = "test2" },
            new(stream, TestFileName, PdfMediaType));
        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a decimal query parameter is formatted correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithDecimal()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "http://foo/withDecimal", ExactQueryParams = [(DecimalQueryKey, "3.456")] },
                Reply.Json("Ok")
            },
        };
        var fixture = handler.CreateClient<IApiWithDecimal>(BaseUrl);

        const decimal val = 3.456M;

        _ = await fixture.GetWithDecimal(val);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a decimal query parameter is formatted correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithDecimalGenerated()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "http://foo/withDecimal", ExactQueryParams = [(DecimalQueryKey, "3.456")] },
                Reply.Json("Ok")
            },
        };
        var fixture = handler.CreateClient<IApiWithDecimal>(BaseUrl);

        const decimal val = 3.456M;

        _ = await fixture.GetWithDecimalGenerated(val);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a path parameter is formatted correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithPathParameterGenerated()
    {
        var handler = new StubHttp
        {
            { Route.Get("http://foo/bar"), Reply.Json("Ok") },
        };
        var fixture = handler.CreateGeneratedClient<IGeneratedParametersApi>(BaseUrl);

        _ = await fixture.GetPath("bar");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a DateOnly path parameter is formatted and substituted by the generated client.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithDateOnlyPathParameterGenerated()
    {
        Uri? captured = null;
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = "*", Reusable = true },
                Reply.From(request =>
                {
                    captured = request.RequestUri;
                    return new(HttpStatusCode.OK) { Content = new StringContent("Ok") };
                })
            },
        };
        var fixture = handler.CreateGeneratedClient<IGeneratedParametersApi>(BaseUrl);

        const int year = 2024;
        const int day = 2;
        var date = new DateOnly(year, 1, day);
        _ = await fixture.GetDateOnlyPath(date);

        var expected = ((IFormattable)date).ToString(null, System.Globalization.CultureInfo.InvariantCulture);
        await Assert.That(captured).IsNotNull();
        await Assert.That(Uri.UnescapeDataString(captured!.ToString()))
            .IsEqualTo($"http://foo/events/{expected}");
    }

    /// <summary>Verifies a query parameter is formatted correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithQueryParameterGenerated()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "http://foo/", ExactQueryParams = [("q", "bar")] },
                Reply.Json("Ok")
            },
        };
        var fixture = handler.CreateGeneratedClient<IGeneratedParametersApi>(BaseUrl);

        _ = await fixture.GetQuery("bar");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies an aliased query parameter is formatted correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithAliasedQueryParameterGenerated()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "http://foo/", ExactQueryParams = [("q", "bar")] },
                Reply.Json("Ok")
            },
        };
        var fixture = handler.CreateGeneratedClient<IGeneratedParametersApi>(BaseUrl);

        _ = await fixture.GetQueryAlias("bar");

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a URL with multiple query parameters is formatted correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithMultipleParametersGenerated()
    {
        const int id = 1;
        const int width = 800;
        const int height = 600;

        var handler = new StubHttp
        {
            { Route.Get("http://foo/1/800x600/foo"), Reply.Json("Ok") },
        };
        var fixture = handler.CreateGeneratedClient<IGeneratedParametersApi>(BaseUrl);

        _ = await fixture.FetchSomethingWithMultipleParametersPerSegment(id, width, height);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a URL with multiple repeated query parameters is formatted correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithMultipleRepeatedParametersGenerated()
    {
        const int id = 1;
        const int size = 300;

        var handler = new StubHttp
        {
            { Route.Get("http://foo/1/300x300/foo"), Reply.Json("Ok") },
        };
        var fixture = handler.CreateGeneratedClient<IGeneratedParametersApi>(BaseUrl);

        _ = await fixture.FetchSomethingWithMultipleRepeatedParametersPerSegment(id, size);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a URL with a nullable parameters is formatted correctly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithNullableParameterGenerated()
    {
        var handler = new StubHttp
        {
            { Route.Get("http://foo/a//b"), Reply.Json("Ok") },
        };
        var fixture = handler.CreateGeneratedClient<IGeneratedParametersApi>(BaseUrl);

        _ = await fixture.GetNullableParam(null);

        await handler.VerifyAllCalledAsync();
    }

    /// <summary>Verifies a round trip URL with a nullable parameter.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetWithNullRoundTripParameterGenerated()
    {
        var handler = new StubHttp
        {
            { Route.Get(BaseUrlWithSlash), Reply.Json("Ok") },
        };
        var fixture = handler.CreateClient<IRoundTrippingNullString>(BaseUrl);

        _ = await fixture.GetValue(null);

        await handler.VerifyAllCalledAsync();
    }
}
