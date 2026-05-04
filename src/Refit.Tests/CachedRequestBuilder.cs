using System.Net;
using System.Net.Http;
using System.Reflection;

using RichardSzalay.MockHttp;


namespace Refit.Tests;

public interface IGeneralRequests
{
    [Post("/foo")]
    Task Empty();

    [Post("/foo")]
    Task SingleParameter(string id);

    [Post("/foo")]
    Task MultiParameter(string id, string name);

    [Post("/foo")]
    Task SingleGenericMultiParameter<TValue>(string id, string name, TValue generic);
}

public interface IDuplicateNames
{
    [Post("/foo")]
    Task SingleParameter(string id);

    [Post("/foo")]
    Task SingleParameter(int id);
}

public class CachedRequestBuilderTests
{
    [Test]
    public void CachedBuilder_ThrowsForNullInnerBuilder()
    {
        Assert.Throws<ArgumentNullException>(() => new CachedRequestBuilderImplementation(null!));
    }

    [Test]
    public void MethodTableKey_ObjectEquals_And_GenericArgumentDifference_AreCovered()
    {
        var key = new MethodTableKey("Foo", [typeof(string)], [typeof(int)]);
        object same = new MethodTableKey("Foo", [typeof(string)], [typeof(int)]);
        object different = new MethodTableKey("Foo", [typeof(string)], [typeof(long)]);
        var differentParameter = new MethodTableKey("Foo", [typeof(int)], [typeof(int)]);

        Assert.True(key.Equals(same));
        Assert.False(key.Equals(different));
        Assert.False(key.Equals(differentParameter));
        Assert.False(key.Equals(new object()));
    }

    [Test]
    public async Task CacheHasCorrectNumberOfElementsTest()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        var fixture = RestService.For<IGeneralRequests>("http://bar", settings);

        // get internal dictionary to check count
        var requestBuilderField = fixture.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Single(x => x.Name == "requestBuilder");
        var requestBuilder = requestBuilderField.GetValue(fixture) as CachedRequestBuilderImplementation;

        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .Respond(HttpStatusCode.OK);
        await fixture.Empty();
        Assert.Single(requestBuilder.MethodDictionary);

        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .WithQueryString("id", "id")
            .Respond(HttpStatusCode.OK);
        await fixture.SingleParameter("id");
        Assert.Equal(2, requestBuilder.MethodDictionary.Count);

        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .WithQueryString("id", "id")
            .WithQueryString("name", "name")
            .Respond(HttpStatusCode.OK);
        await fixture.MultiParameter("id", "name");
        Assert.Equal(3, requestBuilder.MethodDictionary.Count);

        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .WithQueryString("id", "id")
            .WithQueryString("name", "name")
            .WithQueryString("generic", "generic")
            .Respond(HttpStatusCode.OK);
        await fixture.SingleGenericMultiParameter("id", "name", "generic");
        Assert.Equal(4, requestBuilder.MethodDictionary.Count);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task NoDuplicateEntriesTest()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        var fixture = RestService.For<IGeneralRequests>("http://bar", settings);

        // get internal dictionary to check count
        var requestBuilderField = fixture.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Single(x => x.Name == "requestBuilder");
        var requestBuilder = requestBuilderField.GetValue(fixture) as CachedRequestBuilderImplementation;

        // send the same request repeatedly to ensure that multiple dictionary entries are not created
        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .WithQueryString("id", "id")
            .Respond(HttpStatusCode.OK);
        await fixture.SingleParameter("id");
        Assert.Single(requestBuilder.MethodDictionary);

        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .WithQueryString("id", "id")
            .Respond(HttpStatusCode.OK);
        await fixture.SingleParameter("id");
        Assert.Single(requestBuilder.MethodDictionary);

        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .WithQueryString("id", "id")
            .Respond(HttpStatusCode.OK);
        await fixture.SingleParameter("id");
        Assert.Single(requestBuilder.MethodDictionary);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Test]
    public async Task SameNameDuplicateEntriesTest()
    {
        var mockHttp = new MockHttpMessageHandler();
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };

        var fixture = RestService.For<IDuplicateNames>("http://bar", settings);

        // get internal dictionary to check count
        var requestBuilderField = fixture.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Single(x => x.Name == "requestBuilder");
        var requestBuilder = requestBuilderField.GetValue(fixture) as CachedRequestBuilderImplementation;

        // send the two different requests with the same name
        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .WithQueryString("id", "id")
            .Respond(HttpStatusCode.OK);
        await fixture.SingleParameter("id");
        Assert.Single(requestBuilder.MethodDictionary);

        mockHttp
            .Expect(HttpMethod.Post, "http://bar/foo")
            .WithQueryString("id", "10")
            .Respond(HttpStatusCode.OK);
        await fixture.SingleParameter(10);
        Assert.Equal(2, requestBuilder.MethodDictionary.Count);

        mockHttp.VerifyNoOutstandingExpectation();
    }
}
