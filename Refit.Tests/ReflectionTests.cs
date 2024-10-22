using System.Net.Http;
using System.Reflection;
using RichardSzalay.MockHttp;
using Xunit;

namespace Refit.Tests;

public interface IBasicApi
{
    [Get("/{value}")]
    Task<string> GetParam(string value);

    [Get("/{value}")]
    Task<string> GetDerivedParam(BaseRecord value);

    [Get("/{value.PropValue}")]
    Task<string> GetPropertyParam(MyParams value);

    [Get("/{value}")]
    Task<string> GetGenericParam<T>(T value);

    [Get("/")]
    Task<string> GetPropertyQuery(BaseRecord queryKey);

    [Get("/")]
    Task<string> GetDictionaryQuery(IDictionary<string, object> dict);
}

public record DerivedRecord(string Value) : BaseRecord(Value);

public record BaseRecord(string Value);

public record MyParams(string PropValue);

public class TestUrlFormatter : IUrlParameterFormatter
{
    private readonly ICustomAttributeProvider[] expectedAttributeProviders;
    private readonly Type[] expectedTypes;
    private int index = 0;

    public TestUrlFormatter(ICustomAttributeProvider expectedAttributeProvider, Type expectedType)
    {
        expectedAttributeProviders = [expectedAttributeProvider];
        expectedTypes = [expectedType];
    }

    public TestUrlFormatter(ICustomAttributeProvider[] expectedAttributeProviders, Type[] expectedTypes)
    {
        this.expectedAttributeProviders = expectedAttributeProviders;
        this.expectedTypes = expectedTypes;
    }

    public string Format(object value, ICustomAttributeProvider attributeProvider, Type type)
    {
        Assert.Equal(expectedAttributeProviders[index], attributeProvider);
        Assert.Equal(expectedTypes[index], type);
        index++;
        return value.ToString();
    }

    public void AssertNoOutstandingAssertions()
    {
        Assert.Equal(expectedAttributeProviders.Length, index);
        Assert.Equal(expectedTypes.Length, index);
    }
}

public sealed class ReflectionTests : IDisposable
{
    readonly MockHttpMessageHandler mockHandler = new();

    [Fact]
    public async Task UrlParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "http://foo/bar")
            .Respond("application/json", nameof(IBasicApi.GetParam));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetParam))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(string));
        var settings = new RefitSettings() { HttpMessageHandlerFactory = () => mockHandler };
        settings.UrlParameterFormatter = formatter;
        var service = RestService.For<IBasicApi>("http://foo", settings);

        await service.GetParam("bar");
        formatter.AssertNoOutstandingAssertions();
    }

    [Fact]
    public async Task DerivedUrlParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "http://foo/DerivedRecord%20%7B%20Value%20%3D%20Derived%20%7D")
            .Respond("application/json", nameof(IBasicApi.GetDerivedParam));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetDerivedParam))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(BaseRecord));
        var settings = new RefitSettings() { HttpMessageHandlerFactory = () => mockHandler };
        settings.UrlParameterFormatter = formatter;
        var service = RestService.For<IBasicApi>("http://foo", settings);

        await service.GetDerivedParam(new DerivedRecord("Derived"));
        formatter.AssertNoOutstandingAssertions();
    }

    [Fact]
    public async Task PropertyParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "http://foo/propVal")
            .Respond("application/json", nameof(IBasicApi.GetPropertyParam));

        var propertyInfo = typeof(MyParams).GetProperties()[0];

        var formatter = new TestUrlFormatter(propertyInfo, typeof(string));
        var settings = new RefitSettings() { HttpMessageHandlerFactory = () => mockHandler };
        settings.UrlParameterFormatter = formatter;
        var service = RestService.For<IBasicApi>("http://foo", settings);

        await service.GetPropertyParam(new MyParams("propVal"));
        formatter.AssertNoOutstandingAssertions();
    }

    [Fact]
    public async Task GenericParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "http://foo/genericVal")
            .Respond("application/json", nameof(IBasicApi.GetGenericParam));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetGenericParam))!;
        var stringMethod = methodInfo.MakeGenericMethod(typeof(string));
        var parameterInfo = stringMethod.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(string));
        var settings = new RefitSettings() { HttpMessageHandlerFactory = () => mockHandler };
        settings.UrlParameterFormatter = formatter;
        var service = RestService.For<IBasicApi>("http://foo", settings);

        await service.GetGenericParam("genericVal");
        formatter.AssertNoOutstandingAssertions();
    }

    [Fact]
    public async Task QueryPropertyParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "http://foo/")
            .WithExactQueryString(
                new[]
                {
                    new KeyValuePair<string, string>("Value", "queryVal"),
                }
            )
            .Respond("application/json", nameof(IBasicApi.GetPropertyQuery));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetPropertyQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(BaseRecord));
        var settings = new RefitSettings() { HttpMessageHandlerFactory = () => mockHandler };
        settings.UrlParameterFormatter = formatter;
        var service = RestService.For<IBasicApi>("http://foo", settings);

        await service.GetPropertyQuery(new BaseRecord("queryVal"));
        formatter.AssertNoOutstandingAssertions();
    }

    [Fact]
    public async Task DerivedQueryPropertyParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "http://foo/")
            .WithExactQueryString(
                new[]
                {
                    new KeyValuePair<string, string>("Value", "queryVal"),
                }
            )
            .Respond("application/json", nameof(IBasicApi.GetPropertyQuery));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetPropertyQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(BaseRecord));
        var settings = new RefitSettings() { HttpMessageHandlerFactory = () => mockHandler };
        settings.UrlParameterFormatter = formatter;
        var service = RestService.For<IBasicApi>("http://foo", settings);

        await service.GetPropertyQuery(new DerivedRecord("queryVal"));
        formatter.AssertNoOutstandingAssertions();
    }

    [Fact]
    public async Task QueryDictionaryParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "http://foo/")
            .WithExactQueryString(
                new[]
                {
                    new KeyValuePair<string, string>("key0", "1"),
                    new KeyValuePair<string, string>("key1", "2"),
                }
            )
            .Respond("application/json", nameof(IBasicApi.GetDictionaryQuery));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetDictionaryQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter([typeof(string), typeof(string), parameterInfo, parameterInfo], [typeof(string), typeof(string), typeof(IDictionary<string, object>),typeof(IDictionary<string, object>)]);
        var settings = new RefitSettings() { HttpMessageHandlerFactory = () => mockHandler };
        settings.UrlParameterFormatter = formatter;
        var service = RestService.For<IBasicApi>("http://foo", settings);

        var dict = new Dictionary<string, object> { { "key0", 1 }, { "key1", 2 } };
        await service.GetDictionaryQuery(dict);
        formatter.AssertNoOutstandingAssertions();
    }

    public void Dispose()
    {
        mockHandler?.Dispose();
    }
}
