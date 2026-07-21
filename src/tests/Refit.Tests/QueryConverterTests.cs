// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Refit.Tests;

/// <summary>Verifies a <see cref="QueryConverterAttribute"/> flattens a parameter through an <see cref="IQueryConverter{T}"/>.</summary>
public class QueryConverterTests
{
    /// <summary>The base address used by every generated client under test.</summary>
    private const string BaseAddress = "http://api/";

    /// <summary>An age value used to prove non-string entries format correctly.</summary>
    private const int Age = 42;

    /// <summary>A count value used to prove numeric properties format correctly.</summary>
    private const int CountValue = 3;

    /// <summary>Reflection-backed serializer options shared by the interop-converter flattening fixture.</summary>
    private static readonly JsonSerializerOptions ReflectionSerializerOptions =
        new() { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

    /// <summary>Verifies a converter flattens an object-valued dictionary the generator cannot flatten itself.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ConverterFlattensObjectValuedDictionary()
    {
        IDictionary<string, object> filter = new Dictionary<string, object>
        {
            ["name"] = "ada",
            ["age"] = Age,
            ["skip"] = null!
        };

        var generated = await SendAsync(api => api.Search(filter));

        await Assert.That(generated).IsEqualTo("/search?name=ada&age=42");
    }

    /// <summary>Verifies the parameter-level <c>[Query(Prefix)]</c> is passed to the converter as the key prefix.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ConverterReceivesParameterPrefix()
    {
        IDictionary<string, object> filter = new Dictionary<string, object> { ["a"] = 1 };

        var generated = await SendAsync(api => api.SearchPrefixed(filter));

        await Assert.That(generated).IsEqualTo("/prefixed?f.a=1");
    }

    /// <summary>Verifies a null converter argument emits no query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NullConverterArgumentEmitsNoQueryString()
    {
        var generated = await SendAsync(static api => api.Search(null!));

        await Assert.That(generated).IsEqualTo("/search");
    }

    /// <summary>Verifies the System.Text.Json interop converter flattens a POCO, honoring JSON names and nesting.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SystemTextJsonConverterFlattensRegisteredType()
    {
        var settings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(ReflectionSerializerOptions)
        };
        var filter = new StjFilter { Query = "ada", Count = CountValue, Sub = new StjNested { City = "wien" } };

        var generated = await SendAsync(settings, api => api.SearchStj(filter));

        await Assert.That(generated).IsEqualTo("/stj?q=ada&Count=3&Sub.City=wien");
    }

    /// <summary>Verifies the attribute exposes the converter type passed to its constructor.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AttributeExposesConverterType()
    {
        var attribute = new QueryConverterAttribute(typeof(DictionaryObjectQueryConverter));

        await Assert.That(attribute.ConverterType).IsEqualTo(typeof(DictionaryObjectQueryConverter));
    }

    /// <summary>Sends one request through the generated client and returns the relative URI it produced.</summary>
    /// <param name="call">The interface method to invoke.</param>
    /// <returns>The generated request's path and query.</returns>
    private static Task<string> SendAsync(Func<IConverterApi, Task<string>> call) =>
        SendAsync(new RefitSettings(), call);

    /// <summary>Sends one request through the generated client with the given settings and returns its relative URI.</summary>
    /// <param name="settings">The settings to build the client with.</param>
    /// <param name="call">The interface method to invoke.</param>
    /// <returns>The generated request's path and query.</returns>
    private static async Task<string> SendAsync(RefitSettings settings, Func<IConverterApi, Task<string>> call)
    {
        var handler = new TestHttpMessageHandler();
        using var client = HttpClientTestFactory.Create(handler, new(BaseAddress));
        var api = RestService.ForGenerated<IConverterApi>(client, settings);

        _ = await call(api);

        return handler.RequestMessage!.RequestUri!.PathAndQuery;
    }
}
