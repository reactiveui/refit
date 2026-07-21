// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>
/// Verifies that a query object flattened inline by the source generator produces exactly the same URI as the
/// reflection request builder, which walks the same properties at runtime.
/// </summary>
public class QueryObjectFlatteningTests
{
    /// <summary>The base address used by every generated client under test.</summary>
    private const string BaseAddress = "http://api/";

    /// <summary>The identifier assigned to the value-typed query object.</summary>
    private const int StructId = 3;

    /// <summary>Verifies an explicitly marked query object flattens to the reflection builder's query string.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ExplicitQueryObjectFlattensLikeReflection()
    {
        var query = CreateSealedQueryObject();

        await AssertParityAsync(
            "/obj?Name=ada&n=7&Price=5.00&Kept=&Sort=date-desc",
            api => api.FlattenObject(query),
            new RequestBuilderImplementation<IQueryObjectApi>()
                .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenObject)),
            query);
    }

    /// <summary>Verifies an unattributed query object on a body-less method flattens like the reflection builder.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ImpliedQueryObjectFlattensLikeReflection()
    {
        var query = CreateSealedQueryObject();

        await AssertParityAsync(
            "/implied?Name=ada&n=7&Price=5.00&Kept=&Sort=date-desc",
            api => api.FlattenImplied(query),
            new RequestBuilderImplementation<IQueryObjectApi>()
                .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenImplied)),
            query);
    }

    /// <summary>Verifies property-level prefixes and aliases compose exactly as the reflection builder composes them.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PrefixedPropertiesFlattenLikeReflection()
    {
        var query = new PrefixedSealedQueryObject { Zip = "1010", City = "Wien" };

        await AssertParityAsync(
            "/prefixed?addr-Zip=1010&addr-cty=Wien",
            api => api.FlattenPrefixed(query),
            new RequestBuilderImplementation<IQueryObjectApi>()
                .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenPrefixed)),
            query);
    }

    /// <summary>Verifies a value-typed query object flattens like the reflection builder.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task StructQueryObjectFlattensLikeReflection()
    {
        var query = new StructQueryObject { Id = StructId, Tag = "a b" };

        await AssertParityAsync(
            "/struct?Id=3&Tag=a%20b",
            api => api.FlattenStruct(query),
            new RequestBuilderImplementation<IQueryObjectApi>()
                .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenStruct)),
            query);
    }

    /// <summary>Verifies a parameter-level prefix is prepended to every flattened property key.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterPrefixFlattensLikeReflection()
    {
        var query = new StructQueryObject { Id = StructId, Tag = "x" };

        await AssertParityAsync(
            "/paramprefix?root.Id=3&root.Tag=x",
            api => api.FlattenParameterPrefix(query),
            new RequestBuilderImplementation<IQueryObjectApi>()
                .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenParameterPrefix)),
            query);
    }

    /// <summary>
    /// Verifies a derived instance passed through a base-typed parameter contributes only the declared type's
    /// properties, and that this is exactly where generated request building diverges from reflection.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DerivedInstanceFlattensDeclaredPropertiesOnly()
    {
        var query = new DerivedRecordWithProperty("queryName");

        var generated = await SendGeneratedAsync(new RefitSettings(), api => api.FlattenDeclared(query));
        var reflected = await new RequestBuilderImplementation<IQueryObjectApi>()
            .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenDeclared))([query]);

        // The generator sees BaseRecord and emits only its Value property.
        await Assert.That(generated).IsEqualTo("/declared?Value=value");

        // The reflection builder walks the runtime type and additionally emits the derived Name property.
        await Assert.That(reflected.RequestUri!.PathAndQuery).IsEqualTo("/declared?Name=queryName&Value=value");
    }

    /// <summary>
    /// Verifies flattened property keys resolve with the same serializer-aware precedence the reflection builder uses:
    /// <c>[AliasAs]</c> wins, then the content serializer's field name (<c>[JsonPropertyName]</c> for the default
    /// System.Text.Json serializer), then the key formatter.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task JsonNamedPropertiesFlattenLikeReflection()
    {
        var query = new JsonNamedQueryObject { Named = "a", Both = "b", Plain = "c" };

        await AssertParityAsync(
            "/json?json_name=a&alias_wins=b&Plain=c",
            api => api.FlattenJsonNamed(query),
            new RequestBuilderImplementation<IQueryObjectApi>()
                .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenJsonNamed)),
            query);
    }

    /// <summary>
    /// Verifies that disabling <see cref="RefitSettings.HonorContentSerializerPropertyNamesInQuery"/> falls both
    /// builders back to the pre-V14 naming: <c>[JsonPropertyName]</c> is ignored and the CLR name is used, while
    /// <c>[AliasAs]</c> still wins.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task JsonNamedPropertiesFallBackToClrNamesWhenDisabled()
    {
        var settings = new RefitSettings { HonorContentSerializerPropertyNamesInQuery = false };
        var query = new JsonNamedQueryObject { Named = "a", Both = "b", Plain = "c" };

        var generated = await SendGeneratedAsync(settings, api => api.FlattenJsonNamed(query));
        var reflected = await new RequestBuilderImplementation<IQueryObjectApi>(settings)
            .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenJsonNamed))([query]);

        await Assert.That(generated).IsEqualTo("/json?Named=a&alias_wins=b&Plain=c");
        await Assert.That(generated).IsEqualTo(reflected.RequestUri!.PathAndQuery);
    }

    /// <summary>Verifies collection-valued properties flatten to repeated/joined keys exactly as the reflection builder does.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CollectionPropertiesFlattenLikeReflection()
    {
        var query = new CollectionPropertyQueryObject
        {
            Ids = [0, 1],
            Tags = [QuerySort.DateDescending, QuerySort.Name],
            Names = ["a", "b"]
        };

        await AssertParityAsync(
            "/coll?Ids=0%2C1&Tags=date-desc&Tags=Name&n=a%2Cb",
            api => api.FlattenCollections(query),
            new RequestBuilderImplementation<IQueryObjectApi>()
                .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenCollections)),
            query);
    }

    /// <summary>
    /// Verifies that with a customized <see cref="IUrlParameterFormatter"/> the generated slow path reproduces the
    /// reflection builder's two formatting passes for collection properties: each element is formatted, joined (or
    /// kept separate under <c>Multi</c>), then formatted again.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CollectionPropertiesWithCustomFormatterMatchReflection()
    {
        var settings = new RefitSettings { UrlParameterFormatter = new BracketUrlParameterFormatter() };
        var query = new CollectionPropertyQueryObject
        {
            Ids = [0, 1],
            Tags = [QuerySort.Name],
            Names = ["a"]
        };

        var generated = await SendGeneratedAsync(settings, api => api.FlattenCollections(query));
        var reflected = await new RequestBuilderImplementation<IQueryObjectApi>(settings)
            .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenCollections))([query]);

        await Assert.That(generated).IsEqualTo(reflected.RequestUri!.PathAndQuery);
    }

    /// <summary>Verifies a nested object property flattens recursively under a dotted key, exactly as reflection does.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NestedObjectFlattensLikeReflection()
    {
        var query = new NestedQueryObject { Name = "ada", Address = new AddressQuery { City = "wien", Zip = "1010" } };

        await AssertParityAsync(
            "/nested?Name=ada&Address.City=wien&Address.z=1010",
            api => api.FlattenNested(query),
            new RequestBuilderImplementation<IQueryObjectApi>()
                .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenNested)),
            query);
    }

    /// <summary>Verifies a custom key formatter is applied to every nested key segment, matching the reflection builder.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NestedObjectKeysUseCustomKeyFormatter()
    {
        var settings = new RefitSettings { UrlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter() };
        var query = new NestedQueryObject { Name = "ada", Address = new AddressQuery { City = "wien", Zip = "1010" } };

        var generated = await SendGeneratedAsync(settings, api => api.FlattenNested(query));
        var reflected = await new RequestBuilderImplementation<IQueryObjectApi>(settings)
            .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenNested))([query]);

        await Assert.That(generated).IsEqualTo("/nested?name=ada&address.city=wien&address.z=1010");
        await Assert.That(generated).IsEqualTo(reflected.RequestUri!.PathAndQuery);
    }

    /// <summary>Verifies a null nested object contributes no query pairs, matching the reflection builder.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NullNestedObjectEmitsNoNestedKeys()
    {
        var query = new NestedQueryObject { Name = "ada", Address = null };

        await AssertParityAsync(
            "/nested?Name=ada",
            api => api.FlattenNested(query),
            new RequestBuilderImplementation<IQueryObjectApi>()
                .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenNested)),
            query);
    }

    /// <summary>Verifies a non-null nullable nested value type flattens exactly like the reflection builder.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NullableNestedStructWithValueFlattensLikeReflection()
    {
        const double Lat = 1.5;
        const double Lng = 2.5;
        var query = new NullableNestedStructQueryObject { Name = "here", Location = new GeoPoint(Lat, Lng) };

        var generated = await SendGeneratedAsync(new RefitSettings(), api => api.FlattenNullableNestedStruct(query));
        var reflected = await new RequestBuilderImplementation<IQueryObjectApi>()
            .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenNullableNestedStruct))([query]);

        await Assert.That(generated).IsEqualTo(reflected.RequestUri!.PathAndQuery);
    }

    /// <summary>Verifies a null nullable nested value type contributes no query pairs, matching reflection.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NullableNestedStructWhenNullOmittedLikeReflection()
    {
        var query = new NullableNestedStructQueryObject { Name = "here", Location = null };

        var generated = await SendGeneratedAsync(new RefitSettings(), api => api.FlattenNullableNestedStruct(query));
        var reflected = await new RequestBuilderImplementation<IQueryObjectApi>()
            .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenNullableNestedStruct))([query]);

        await Assert.That(generated).IsEqualTo(reflected.RequestUri!.PathAndQuery);
    }

    /// <summary>Verifies dictionary properties inside a query object expand exactly like the reflection builder.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DictionaryPropertyExpandsLikeReflection()
    {
        const int Count = 10;
        var query = new DictionaryPropertyQueryObject { Name = "root" };
        query.Tags["a"] = "1";
        query.Tags["b"] = "2";
        query.Counts["x"] = Count;

        var generated = await SendGeneratedAsync(new RefitSettings(), api => api.FlattenDictionaryProperty(query));
        var reflected = await new RequestBuilderImplementation<IQueryObjectApi>()
            .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenDictionaryProperty))([query]);

        await Assert.That(generated).IsEqualTo(reflected.RequestUri!.PathAndQuery);
    }

    /// <summary>Verifies a dictionary expands to one query pair per entry, exactly as the reflection builder does.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DictionaryExpandsLikeReflection()
    {
        IDictionary<string, string> query = new Dictionary<string, string>
        {
            ["key0"] = "a b",
            ["key1"] = "two"
        };

        await AssertParityAsync(
            "/dict?key0=a%20b&key1=two",
            api => api.ExpandDictionary(query),
            new RequestBuilderImplementation<IQueryObjectApi>()
                .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.ExpandDictionary)),
            query);
    }

    /// <summary>Verifies an enum-keyed dictionary renders its keys through the enum member value.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task EnumKeyedDictionaryExpandsLikeReflection()
    {
        const int firstValue = 10;
        const int secondValue = 20;
        var query = new Dictionary<QuerySort, int>
        {
            [QuerySort.DateDescending] = firstValue,
            [QuerySort.Name] = secondValue
        };

        await AssertParityAsync(
            "/dict/enum?date-desc=10&Name=20",
            api => api.ExpandEnumKeyedDictionary(query),
            new RequestBuilderImplementation<IQueryObjectApi>()
                .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.ExpandEnumKeyedDictionary)),
            query);
    }

    /// <summary>Verifies a parameter-level prefix is prepended to every dictionary key.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PrefixedDictionaryExpandsLikeReflection()
    {
        IDictionary<string, string> query = new Dictionary<string, string> { ["a"] = "1" };

        await AssertParityAsync(
            "/dict/prefixed?root.a=1",
            api => api.ExpandPrefixedDictionary(query),
            new RequestBuilderImplementation<IQueryObjectApi>()
                .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.ExpandPrefixedDictionary)),
            query);
    }

    /// <summary>Verifies a dictionary entry with a null value is omitted, matching the reflection builder.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DictionaryOmitsNullValuedEntries()
    {
        IDictionary<string, string> query = new Dictionary<string, string>
        {
            ["kept"] = "yes",
            ["dropped"] = null!
        };

        await AssertParityAsync(
            "/dict?kept=yes",
            api => api.ExpandDictionary(query),
            new RequestBuilderImplementation<IQueryObjectApi>()
                .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.ExpandDictionary)),
            query);
    }

    /// <summary>Verifies a custom key formatter is applied to unaliased property names only.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CustomKeyFormatterAppliesToUnaliasedPropertiesOnly()
    {
        var settings = new RefitSettings { UrlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter() };
        var query = new PrefixedSealedQueryObject { Zip = "1010", City = "Wien" };

        var generated = await SendGeneratedAsync(settings, api => api.FlattenPrefixed(query));
        var reflected = await new RequestBuilderImplementation<IQueryObjectApi>(settings)
            .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenPrefixed))([query]);

        await Assert.That(generated).IsEqualTo("/prefixed?addr-zip=1010&addr-cty=Wien");
        await Assert.That(generated).IsEqualTo(reflected.RequestUri!.PathAndQuery);
    }

    /// <summary>Verifies a null query object emits no query pairs at all.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NullQueryObjectEmitsNoQueryString()
    {
        var generated = await SendGeneratedAsync(new RefitSettings(), static api => api.FlattenObject(null!));
        var reflected = await new RequestBuilderImplementation<IQueryObjectApi>()
            .BuildRequestFactoryForMethod(nameof(IQueryObjectApi.FlattenObject))([null!]);

        await Assert.That(generated).IsEqualTo("/obj");
        await Assert.That(generated).IsEqualTo(reflected.RequestUri!.PathAndQuery);
    }

    /// <summary>Builds the canonical query object used by the flattening tests.</summary>
    /// <returns>A populated query object.</returns>
    private static SealedQueryObject CreateSealedQueryObject()
    {
        const int number = 7;
        const double price = 5D;

        return new()
        {
            Name = "ada",
            Number = number,
            Price = price,
            Kept = null,
            Skipped = null,
            Sort = QuerySort.DateDescending,
            Hidden = "hidden",
            Secret = "secret"
        };
    }

    /// <summary>Sends one request through the source-generated client and returns the relative URI it produced.</summary>
    /// <param name="settings">The settings to build the client with.</param>
    /// <param name="call">The interface method to invoke.</param>
    /// <returns>The generated request's path and query.</returns>
    private static async Task<string> SendGeneratedAsync(RefitSettings settings, Func<IQueryObjectApi, Task<string>> call)
    {
        var handler = new TestHttpMessageHandler();
        using var client = HttpClientTestFactory.Create(handler, new(BaseAddress));
        var api = RestService.ForGenerated<IQueryObjectApi>(client, settings);

        _ = await call(api);

        return handler.RequestMessage!.RequestUri!.PathAndQuery;
    }

    /// <summary>Asserts the generated and reflection request builders produce the same expected relative URI.</summary>
    /// <param name="expectedPathAndQuery">The expected path and query.</param>
    /// <param name="call">The interface method to invoke on the generated client.</param>
    /// <param name="reflectionFactory">The reflection builder's request factory for the same method.</param>
    /// <param name="argument">The single argument passed to the reflection factory.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private static async Task AssertParityAsync(
        string expectedPathAndQuery,
        Func<IQueryObjectApi, Task<string>> call,
        Func<object[], Task<HttpRequestMessage>> reflectionFactory,
        object argument)
    {
        var generated = await SendGeneratedAsync(new RefitSettings(), call);
        var reflected = await reflectionFactory([argument]);

        await Assert.That(generated).IsEqualTo(expectedPathAndQuery);
        await Assert.That(reflected.RequestUri!.PathAndQuery).IsEqualTo(expectedPathAndQuery);
    }
}
