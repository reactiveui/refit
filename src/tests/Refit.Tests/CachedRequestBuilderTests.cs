// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Reflection;

using RichardSzalay.MockHttp;

namespace Refit.Tests;

/// <summary>Tests for the cached request builder implementation.</summary>
[RequiresUnreferencedCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
[RequiresDynamicCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
public class CachedRequestBuilderTests
{
    /// <summary>Verifies the cached builder throws when constructed with a null inner builder.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CachedBuilder_ThrowsForNullInnerBuilder()
    {
        await Assert.That(() => new CachedRequestBuilderImplementation(null!)).ThrowsExactly<ArgumentNullException>();
    }

    /// <summary>Verifies method-table key equality, including object equality and generic-argument differences.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MethodTableKey_ObjectEquals_And_GenericArgumentDifference_AreCovered()
    {
        var key = new MethodTableKey("Foo", [typeof(string)], [typeof(int)]);
        object same = new MethodTableKey("Foo", [typeof(string)], [typeof(int)]);
        object different = new MethodTableKey("Foo", [typeof(string)], [typeof(long)]);
        var differentParameter = new MethodTableKey("Foo", [typeof(int)], [typeof(int)]);

        await Assert.That(key.Equals(same)).IsTrue();
        await Assert.That(key.Equals(different)).IsFalse();
        await Assert.That(key.Equals(differentParameter)).IsFalse();
        await Assert.That(key.Equals(new object())).IsFalse();
    }

    /// <summary>Verifies the cache grows by one entry per distinct method invocation.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CacheHasCorrectNumberOfElementsTest()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        var fixture = RestService.For<IGeneralRequests>("http://bar", settings);

        // get internal dictionary to check count
        var requestBuilderField = fixture.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Single(x => x.Name == "requestBuilder");
        var requestBuilder = (CachedRequestBuilderImplementation)requestBuilderField.GetValue(fixture)!;

        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .Respond(HttpStatusCode.OK);
        await fixture.Empty();
        await Assert.That(requestBuilder.MethodDictionary).HasSingleItem();

        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .WithQueryString("id", "id")
            .Respond(HttpStatusCode.OK);
        await fixture.SingleParameter("id");
        await Assert.That(requestBuilder.MethodDictionary.Count).IsEqualTo(2);

        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .WithQueryString("id", "id")
            .WithQueryString("name", "name")
            .Respond(HttpStatusCode.OK);
        await fixture.MultiParameter("id", "name");
        await Assert.That(requestBuilder.MethodDictionary.Count).IsEqualTo(3);

        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .WithQueryString("id", "id")
            .WithQueryString("name", "name")
            .WithQueryString("generic", "generic")
            .Respond(HttpStatusCode.OK);
        await fixture.SingleGenericMultiParameter("id", "name", "generic");
        await Assert.That(requestBuilder.MethodDictionary.Count).IsEqualTo(4);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies repeated identical requests do not create duplicate cache entries.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NoDuplicateEntriesTest()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        var fixture = RestService.For<IGeneralRequests>("http://bar", settings);

        // get internal dictionary to check count
        var requestBuilderField = fixture.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Single(x => x.Name == "requestBuilder");
        var requestBuilder = (CachedRequestBuilderImplementation)requestBuilderField.GetValue(fixture)!;

        // send the same request repeatedly to ensure that multiple dictionary entries are not created
        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .WithQueryString("id", "id")
            .Respond(HttpStatusCode.OK);
        await fixture.SingleParameter("id");
        await Assert.That(requestBuilder.MethodDictionary).HasSingleItem();

        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .WithQueryString("id", "id")
            .Respond(HttpStatusCode.OK);
        await fixture.SingleParameter("id");
        await Assert.That(requestBuilder.MethodDictionary).HasSingleItem();

        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .WithQueryString("id", "id")
            .Respond(HttpStatusCode.OK);
        await fixture.SingleParameter("id");
        await Assert.That(requestBuilder.MethodDictionary).HasSingleItem();

        mockHttp.VerifyNoOutstandingExpectation();
    }

    /// <summary>Verifies same-named overloads with different parameter types produce distinct cache entries.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SameNameDuplicateEntriesTest()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        var fixture = RestService.For<IDuplicateNames>("http://bar", settings);

        // get internal dictionary to check count
        var requestBuilderField = fixture.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Single(x => x.Name == "requestBuilder");
        var requestBuilder = (CachedRequestBuilderImplementation)requestBuilderField.GetValue(fixture)!;

        // send the two different requests with the same name
        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .WithQueryString("id", "id")
            .Respond(HttpStatusCode.OK);
        await fixture.SingleParameter("id");
        await Assert.That(requestBuilder.MethodDictionary).HasSingleItem();

        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .WithQueryString("id", "10")
            .Respond(HttpStatusCode.OK);
        await fixture.SingleParameter(10);
        await Assert.That(requestBuilder.MethodDictionary.Count).IsEqualTo(2);

        mockHttp.VerifyNoOutstandingExpectation();
    }
}
