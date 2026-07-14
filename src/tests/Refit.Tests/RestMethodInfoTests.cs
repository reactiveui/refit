// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Tests for <see cref="RestMethodInfoInternal"/> parameter and header parsing.</summary>
public partial class RestMethodInfoTests
{
    /// <summary>The base address used to resolve relative request URIs in the query tests.</summary>
    private const string BaseAddressUri = "http://api";

    /// <summary>The User-Agent header name asserted across the header tests.</summary>
    private const string UserAgentHeader = "User-Agent";

    /// <summary>Filter values used by path-bound object query tests.</summary>
    private static readonly string[] _filterValues = ["A", "B"];

    /// <summary>Sample integer collection used to exercise inner-collection query formatting.</summary>
    private static readonly int[] _testCollectionValues = [1, 2, 3];

    /// <summary>Verifies a method with a garbage path throws an argument exception.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task GarbagePathsShouldThrow()
    {
        var shouldDie = true;

        try
        {
            var input = typeof(IRestMethodInfoTests);
            _ = new RestMethodInfoInternal(
                input,
                input
                    .GetMethods()
                    .First(static x => x.Name == nameof(IRestMethodInfoTests.GarbagePath)));
        }
        catch (ArgumentException)
        {
            shouldDie = false;
        }

        await Assert.That(shouldDie).IsFalse();
    }

    /// <summary>Verifies a method with missing parameters throws an argument exception.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MissingParametersShouldBlowUp()
    {
        var shouldDie = true;

        try
        {
            var input = typeof(IRestMethodInfoTests);
            _ = new RestMethodInfoInternal(
                input,
                input
                    .GetMethods()
                    .First(
                        static x =>
                            x.Name
                            == nameof(IRestMethodInfoTests.FetchSomeStuffMissingParameters)));
        }
        catch (ArgumentException)
        {
            shouldDie = false;
        }

        await Assert.That(shouldDie).IsFalse();
    }

    /// <summary>Verifies basic parameter mapping resolves names and types correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterMappingSmokeTest()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(static x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuff)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies enabling unmatched route parameters short-circuits the route-binding leniency flag before it
    /// consults whether the method is a generic definition.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AllowUnmatchedRouteParametersEnablesLenientRouteBinding()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(static x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuff)),
            new RefitSettings { AllowUnmatchedRouteParameters = true });

        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
    }

    /// <summary>Verifies parameter mapping when the same id appears in a few places.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterMappingWithTheSameIdInAFewPlaces()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(static x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithTheSameId)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies parameter mapping when the same id appears in the query parameter.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterMappingWithTheSameIdInTheQueryParameter()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    static x =>
                        x.Name
                        == nameof(
                            IRestMethodInfoTests.FetchSomeStuffWithTheIdInAParameterMultipleTimes)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies parameter mapping for a round-tripping parameter.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterMappingWithRoundTrippingSmokeTest()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    static x =>
                        x.Name
                        == nameof(IRestMethodInfoTests.FetchSomeStuffWithRoundTrippingParam)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("path");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.RoundTripping);
        await Assert.That(fixture.ParameterMap[1].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[1].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies parameter mapping with a query parameter.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterMappingWithQuerySmokeTest()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(static x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithQueryParam)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap[1]).IsEqualTo("search");
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies parameter mapping with a hardcoded query parameter.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterMappingWithHardcodedQuerySmokeTest()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    static x =>
                        x.Name
                        == nameof(IRestMethodInfoTests.FetchSomeStuffWithHardcodedQueryParam)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies an alias on a parameter maps correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AliasMappingShouldWork()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(static x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithAlias)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies multiple parameters per URL segment map correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task MultipleParametersPerSegmentShouldWork()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(static x => x.Name == nameof(IRestMethodInfoTests.FetchAnImage)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("width");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.ParameterMap[1].Name).IsEqualTo("height");
        await Assert.That(fixture.ParameterMap[1].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
    }

    /// <summary>Verifies the body parameter is located and indexed correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task FindTheBodyParameter()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(static x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithBody)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);

        await Assert.That(fixture.BodyParameterInfo).IsNotNull();
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo!.Item3).IsEqualTo(1);
    }

    /// <summary>Verifies the authorize parameter is located and indexed correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task FindTheAuthorizeParameter()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    static x =>
                        x.Name
                        == nameof(
                            IRestMethodInfoTests.FetchSomeStuffWithAuthorizationSchemeSpecified)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);

        await Assert.That(fixture.AuthorizeParameterInfo).IsNotNull();
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.AuthorizeParameterInfo!.Item2).IsEqualTo(1);
    }

    /// <summary>Verifies URL-encoded content is allowed and reflected in the body serialization method.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AllowUrlEncodedContent()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(static x => x.Name == nameof(IRestMethodInfoTests.PostSomeUrlEncodedStuff)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);

        await Assert.That(fixture.BodyParameterInfo).IsNotNull();
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo!.Item1).IsEqualTo(BodySerializationMethod.UrlEncoded);
    }

    /// <summary>Verifies hardcoded headers are parsed into the headers dictionary.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task HardcodedHeadersShouldWork()
    {
        const int expectedHeaderCount = 3;
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    static x =>
                        x.Name
                        == nameof(IRestMethodInfoTests.FetchSomeStuffWithHardcodedHeaders)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();

        await Assert.That(fixture.Headers.ContainsKey("Api-Version")).IsTrue().Because("Headers include Api-Version header");
        await Assert.That(fixture.Headers["Api-Version"]).IsEqualTo("2");
        await Assert.That(fixture.Headers.ContainsKey(UserAgentHeader)).IsTrue().Because("Headers include User-Agent header");
        await Assert.That(fixture.Headers[UserAgentHeader]).IsEqualTo("RefitTestClient");
        await Assert.That(fixture.Headers.ContainsKey("Accept")).IsTrue().Because("Headers include Accept header");
        await Assert.That(fixture.Headers["Accept"]).IsEqualTo("application/json");
        await Assert.That(fixture.Headers.Count).IsEqualTo(expectedHeaderCount);
    }

    /// <summary>Verifies dynamic headers are mapped and merged with hardcoded headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DynamicHeadersShouldWork()
    {
        const int expectedHeaderCount = 2;
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    static x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuffWithDynamicHeader)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.PropertyParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();

        await Assert.That(fixture.HeaderParameterMap[1]).IsEqualTo("Authorization");
        await Assert.That(fixture.Headers.ContainsKey(UserAgentHeader)).IsTrue().Because("Headers include User-Agent header");
        await Assert.That(fixture.Headers[UserAgentHeader]).IsEqualTo("RefitTestClient");
        await Assert.That(fixture.Headers.Count).IsEqualTo(expectedHeaderCount);
    }

    /// <summary>Verifies constructor guard and missing HTTP method branches.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ConstructorGuardsAndMissingHttpMethodThrow()
    {
        var input = typeof(IRestMethodInfoTests);
        var method = input.GetMethods().First(static x => x.Name == nameof(IRestMethodInfoTests.FetchSomeStuff));
        var missingInput = typeof(IMissingHttpMethodApi);
        var missingHttpMethod = missingInput.GetMethod(nameof(IMissingHttpMethodApi.MethodWithoutHttpMethod))!;

        await Assert.That(() => new RestMethodInfoInternal(null!, method))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => new RestMethodInfoInternal(input, null!))
            .ThrowsExactly<ArgumentNullException>();
        await Assert.That(() => new RestMethodInfoInternal(missingInput, missingHttpMethod))
            .ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Verifies CR/LF paths are rejected.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task PathContainingNewlineThrows()
    {
        var input = typeof(IRestMethodInfoTests);
        var method = input.GetMethods().First(static x => x.Name == nameof(IRestMethodInfoTests.NewlinePath));

        await Assert.That(() => new RestMethodInfoInternal(input, method))
            .ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies object route binding ignores unreadable public properties.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ObjectRouteBindingIgnoresUnreadableProperties()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(static x => x.Name == nameof(IRestMethodInfoTests.ObjectRouteWithUnreadableProperty)));

        await Assert.That(fixture.ParameterMap).HasSingleItem();
        await Assert.That(fixture.ParameterMap[0].IsObjectPropertyParameter).IsTrue();
        await Assert.That(fixture.ParameterMap[0].ParameterProperties).HasSingleItem();
        await Assert.That(fixture.ParameterMap[0].ParameterProperties[0].PropertyInfo.Name)
            .IsEqualTo(nameof(RouteObjectWithUnreadableProperty.Visible));
    }

    /// <summary>Verifies a path cannot bind a parameter both directly and through one of its properties.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ObjectRouteBindingConflictingDirectAndPropertyMatchThrows()
    {
        var input = typeof(IRestMethodInfoTests);
        var method = input.GetMethods().First(static x => x.Name == nameof(IRestMethodInfoTests.ConflictingObjectRoute));

        await Assert.That(() => new RestMethodInfoInternal(input, method))
            .ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies a deep dotted route placeholder whose root segment matches no parameter is rejected.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DeepDottedRouteWithUnknownRootParameterThrows()
    {
        var input = typeof(IRestMethodInfoTests);
        var method = input.GetMethods().First(static x => x.Name == nameof(IRestMethodInfoTests.DeepDottedRouteWithUnknownRootParameter));

        await Assert.That(() => new RestMethodInfoInternal(input, method))
            .ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies a deep dotted route placeholder rooted on a value-type parameter is rejected, because a
    /// value-type parameter cannot own a nested property chain.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DeepDottedRouteWithValueTypeRootParameterThrows()
    {
        var input = typeof(IRestMethodInfoTests);
        var method = input.GetMethods().First(static x => x.Name == nameof(IRestMethodInfoTests.DeepDottedRouteWithValueTypeRootParameter));

        await Assert.That(() => new RestMethodInfoInternal(input, method))
            .ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies a deep dotted route placeholder whose final nested property does not exist is rejected.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DeepDottedRouteWithUnknownNestedPropertyThrows()
    {
        var input = typeof(IRestMethodInfoTests);
        var method = input.GetMethods().First(static x => x.Name == nameof(IRestMethodInfoTests.DeepDottedRouteWithUnknownNestedProperty));

        await Assert.That(() => new RestMethodInfoInternal(input, method))
            .ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies only one authorization parameter may be declared.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DuplicateAuthorizeParametersThrow()
    {
        var input = typeof(IRestMethodInfoTests);
        var method = input.GetMethods().First(static x => x.Name == nameof(IRestMethodInfoTests.DuplicateAuthorize));

        await Assert.That(() => new RestMethodInfoInternal(input, method))
            .ThrowsExactly<ArgumentException>();
    }
}
