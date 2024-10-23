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
    Task<string> GetQuery(string queryKey);

    [Get("/")]
    Task<string> GetGenericQuery<T>(T queryKey);

    [Get("/")]
    Task<string> GetPropertyQuery(BaseRecord queryKey);

    [Get("/")]
    Task<string> GetEnumerableQuery(IEnumerable<string> enums);

    [Get("/")]
    Task<string> GetEnumerablePropertyQuery(MyEnumerableParams enums);

    [Get("/")]
    Task<string> GetDictionaryQuery(IDictionary<string, object> dict);
}

public record DerivedRecordWithProperty(string Name) : BaseRecord("value");

public record DerivedRecord(string Value) : BaseRecord(Value);

public record BaseRecord(string Value);

public record MyParams(string PropValue);

public record MyEnumerableParams(int[] Enumerable);

public class TestUrlFormatter : IUrlParameterFormatter
{
    private readonly ICustomAttributeProvider[] expectedAttributeProviders;
    private readonly Type[] expectedTypes;
    private int index;

    public TestUrlFormatter(ICustomAttributeProvider expectedAttributeProvider, Type expectedType)
    {
        expectedAttributeProviders = [expectedAttributeProvider];
        expectedTypes = [expectedType];
    }

    public TestUrlFormatter(
        ICustomAttributeProvider[] expectedAttributeProviders,
        Type[] expectedTypes
    )
    {
        this.expectedAttributeProviders = expectedAttributeProviders;
        this.expectedTypes = expectedTypes;
    }

    public string Format(object value, ICustomAttributeProvider attributeProvider, Type type)
    {
        Assert.Equal(expectedAttributeProviders[index], attributeProvider);
        Assert.Equal(expectedTypes[index], type);
        index++;
        return value!.ToString();
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
            .Expect(HttpMethod.Get, "https://foo/bar")
            .Respond("application/json", nameof(IBasicApi.GetParam));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetParam))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(string));
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetParam("bar");
        formatter.AssertNoOutstandingAssertions();
    }

    [Fact]
    public async Task DerivedUrlParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "https://foo/DerivedRecord%20%7B%20Value%20%3D%20Derived%20%7D")
            .Respond("application/json", nameof(IBasicApi.GetDerivedParam));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetDerivedParam))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(BaseRecord));
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetDerivedParam(new DerivedRecord("Derived"));
        formatter.AssertNoOutstandingAssertions();
    }

    [Fact]
    public async Task PropertyParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "https://foo/propVal")
            .Respond("application/json", nameof(IBasicApi.GetPropertyParam));

        var propertyInfo = typeof(MyParams).GetProperties()[0];

        var formatter = new TestUrlFormatter(propertyInfo, typeof(string));
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetPropertyParam(new MyParams("propVal"));
        formatter.AssertNoOutstandingAssertions();
    }

    [Fact]
    public async Task GenericParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "https://foo/genericVal")
            .Respond("application/json", nameof(IBasicApi.GetGenericParam));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetGenericParam))!;
        var stringMethod = methodInfo.MakeGenericMethod(typeof(string));
        var parameterInfo = stringMethod.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(string));
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetGenericParam("genericVal");
        formatter.AssertNoOutstandingAssertions();
    }

    [Fact]
    public async Task QueryParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "https://foo/")
            .WithExactQueryString(
                new[] { new KeyValuePair<string, string>("queryKey", "queryValue"), }
            )
            .Respond("application/json", nameof(IBasicApi.GetQuery));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(string));
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetQuery("queryValue");
        formatter.AssertNoOutstandingAssertions();
    }

    [Fact]
    public async Task QueryPropertyParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "https://foo/")
            .WithExactQueryString(new[] { new KeyValuePair<string, string>("Value", "queryVal"), })
            .Respond("application/json", nameof(IBasicApi.GetPropertyQuery));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetPropertyQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(BaseRecord));
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetPropertyQuery(new BaseRecord("queryVal"));
        formatter.AssertNoOutstandingAssertions();
    }

    [Fact]
    public async Task DerivedQueryPropertyParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "https://foo/")
            .WithExactQueryString(
                new[]
                {
                    new KeyValuePair<string, string>("Name", "queryName"),
                    new KeyValuePair<string, string>("Value", "value"),
                }
            )
            .Respond("application/json", nameof(IBasicApi.GetPropertyQuery));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetPropertyQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(
            [parameterInfo, parameterInfo],
            [typeof(BaseRecord), typeof(BaseRecord)]
        );
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetPropertyQuery(new DerivedRecordWithProperty("queryName"));
        formatter.AssertNoOutstandingAssertions();
    }

    [Fact]
    public async Task GenericQueryParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "https://foo/")
            .WithExactQueryString(
                new[] { new KeyValuePair<string, string>("queryKey", "queryValue"), }
            )
            .Respond("application/json", nameof(IBasicApi.GetGenericQuery));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetGenericQuery))!;
        var stringMethod = methodInfo.MakeGenericMethod(typeof(string));
        var parameterInfo = stringMethod.GetParameters()[0];

        var formatter = new TestUrlFormatter(parameterInfo, typeof(string));
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetGenericQuery("queryValue");
        formatter.AssertNoOutstandingAssertions();
    }

    [Fact]
    public async Task EnumerableQueryParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "https://foo/")
            .WithExactQueryString(new[] { new KeyValuePair<string, string>("enums", "k0,k1"), })
            .Respond("application/json", nameof(IBasicApi.GetEnumerableQuery));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetEnumerableQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];

        var formatter = new TestUrlFormatter(
            [parameterInfo, parameterInfo],
            [typeof(IEnumerable<string>), typeof(IEnumerable<string>)]
        );
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetEnumerableQuery(["k0", "k1"]);
        formatter.AssertNoOutstandingAssertions();
    }

    [Fact]
    public async Task EnumerablePropertyQueryParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "https://foo/")
            .WithExactQueryString(new[] { new KeyValuePair<string, string>("Enumerable", "0,1"), })
            .Respond("application/json", nameof(IBasicApi.GetEnumerablePropertyQuery));

        var methodInfo = typeof(IBasicApi).GetMethod(nameof(IBasicApi.GetEnumerablePropertyQuery))!;
        var parameterInfo = methodInfo.GetParameters()[0];
        var propertyInfo = typeof(MyEnumerableParams).GetProperties()[0];

        var formatter = new TestUrlFormatter(
            [propertyInfo, propertyInfo, parameterInfo],
            [typeof(int[]), typeof(int[]), typeof(MyEnumerableParams)]
        );
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        await service.GetEnumerablePropertyQuery(new MyEnumerableParams([0, 1]));
        formatter.AssertNoOutstandingAssertions();
    }

    [Fact]
    public async Task QueryDictionaryParameterShouldBeExpectedReflection()
    {
        mockHandler
            .Expect(HttpMethod.Get, "https://foo/")
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

        var formatter = new TestUrlFormatter(
            [typeof(string), typeof(string), parameterInfo, parameterInfo],
            [
                typeof(string),
                typeof(string),
                typeof(IDictionary<string, object>),
                typeof(IDictionary<string, object>)
            ]
        );
        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => mockHandler,
            UrlParameterFormatter = formatter
        };
        var service = RestService.For<IBasicApi>("https://foo", settings);

        var dict = new Dictionary<string, object> { { "key0", 1 }, { "key1", 2 } };
        await service.GetDictionaryQuery(dict);
        formatter.AssertNoOutstandingAssertions();
    }

    public void Dispose()
    {
        mockHandler?.Dispose();
    }
}
