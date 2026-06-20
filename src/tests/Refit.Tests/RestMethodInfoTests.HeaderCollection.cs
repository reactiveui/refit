// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reflection;

namespace Refit.Tests;

/// <summary>Tests for <see cref="RestMethodInfoInternal"/> header-collection and property parsing.</summary>
public partial class RestMethodInfoTests
{
    /// <summary>Verifies a dynamic header collection is parsed and merged with hardcoded headers.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DynamicHeaderCollectionShouldWork()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    x =>
                        x.Name
                        == nameof(
                            IRestMethodInfoTests.FetchSomeStuffWithDynamicHeaderCollection)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.HeaderParameterMap).IsEmpty();
        await Assert.That(fixture.PropertyParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();

        await Assert.That(fixture.Headers.ContainsKey("Authorization")).IsTrue().Because("Headers include Authorization header");
        await Assert.That(fixture.Headers["Authorization"]).IsEqualTo("SRSLY aHR0cDovL2kuaW1ndXIuY29tL0NGRzJaLmdpZg==");
        await Assert.That(fixture.Headers.ContainsKey("Accept")).IsTrue().Because("Headers include Accept header");
        await Assert.That(fixture.Headers["Accept"]).IsEqualTo("application/json");
        await Assert.That(fixture.Headers.ContainsKey("User-Agent")).IsTrue().Because("Headers include User-Agent header");
        await Assert.That(fixture.Headers["User-Agent"]).IsEqualTo("RefitTestClient");
        await Assert.That(fixture.Headers.ContainsKey("Api-Version")).IsTrue().Because("Headers include Api-Version header");
        await Assert.That(fixture.Headers["Api-Version"]).IsEqualTo("1");

        await Assert.That(fixture.Headers.Count).IsEqualTo(4);
        await Assert.That(fixture.HasHeaderCollection).IsTrue();
        await Assert.That(fixture.HeaderCollectionAt(1)).IsTrue();
    }

    /// <summary>Verifies a dynamic header collection works alongside a body for put, post and patch.</summary>
    /// <param name="interfaceMethodName">The interface method to build the rest method info for.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(nameof(IRestMethodInfoTests.PutSomeStuffWithCustomHeaderCollection))]
    [Arguments(nameof(IRestMethodInfoTests.PostSomeStuffWithCustomHeaderCollection))]
    [Arguments(nameof(IRestMethodInfoTests.PatchSomeStuffWithCustomHeaderCollection))]
    public async Task DynamicHeaderCollectionShouldWorkWithBody(string interfaceMethodName)
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == interfaceMethodName));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.HeaderParameterMap).IsEmpty();
        await Assert.That(fixture.PropertyParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNotNull();
        await Assert.That(fixture.AuthorizeParameterInfo).IsNull();

        await Assert.That(fixture.HasHeaderCollection).IsTrue();
        await Assert.That(fixture.HeaderCollectionAt(2)).IsTrue();
    }

    /// <summary>Verifies a dynamic header collection works without a body for put, post and patch.</summary>
    /// <param name="interfaceMethodName">The interface method to build the rest method info for.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(nameof(IRestMethodInfoTests.PutSomeStuffWithoutBodyAndCustomHeaderCollection))]
    [Arguments(nameof(IRestMethodInfoTests.PostSomeStuffWithoutBodyAndCustomHeaderCollection))]
    [Arguments(nameof(IRestMethodInfoTests.PatchSomeStuffWithoutBodyAndCustomHeaderCollection))]
    public async Task DynamicHeaderCollectionShouldWorkWithoutBody(string interfaceMethodName)
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == interfaceMethodName));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.HeaderParameterMap).IsEmpty();
        await Assert.That(fixture.PropertyParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
        await Assert.That(fixture.AuthorizeParameterInfo).IsNull();

        await Assert.That(fixture.HasHeaderCollection).IsTrue();
        await Assert.That(fixture.HeaderCollectionAt(1)).IsTrue();
    }

    /// <summary>Verifies a dynamic header collection works with an inferred body for put, post and patch.</summary>
    /// <param name="interfaceMethodName">The interface method to build the rest method info for.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(nameof(IRestMethodInfoTests.PutSomeStuffWithInferredBodyAndWithDynamicHeaderCollection))]
    [Arguments(nameof(IRestMethodInfoTests.PostSomeStuffWithInferredBodyAndWithDynamicHeaderCollection))]
    [Arguments(nameof(IRestMethodInfoTests.PatchSomeStuffWithInferredBodyAndWithDynamicHeaderCollection))]
    public async Task DynamicHeaderCollectionShouldWorkWithInferredBody(string interfaceMethodName)
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == interfaceMethodName));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.HeaderParameterMap).IsEmpty();
        await Assert.That(fixture.PropertyParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNotNull();
        await Assert.That(fixture.AuthorizeParameterInfo).IsNull();

        await Assert.That(fixture.HasHeaderCollection).IsTrue();
        await Assert.That(fixture.HeaderCollectionAt(1)).IsTrue();
        await Assert.That(fixture.BodyParameterInfo!.Item3).IsEqualTo(2);
    }

    /// <summary>Verifies a dynamic header collection works alongside an authorize parameter.</summary>
    /// <param name="interfaceMethodName">The interface method to build the rest method info for.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(nameof(IRestMethodInfoTests.FetchSomeStuffWithDynamicHeaderCollectionAndAuthorize))]
    [Arguments(nameof(IRestMethodInfoTests.PostSomeStuffWithDynamicHeaderCollectionAndAuthorize))]
    public async Task DynamicHeaderCollectionShouldWorkWithAuthorize(string interfaceMethodName)
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == interfaceMethodName));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.HeaderParameterMap).IsEmpty();
        await Assert.That(fixture.PropertyParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();

        await Assert.That(fixture.AuthorizeParameterInfo).IsNotNull();
        await Assert.That(fixture.HasHeaderCollection).IsTrue();
        await Assert.That(fixture.HeaderCollectionAt(2)).IsTrue();
    }

    /// <summary>Verifies a dynamic header collection works alongside a dynamic header in either order.</summary>
    /// <param name="interfaceMethodName">The interface method to build the rest method info for.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(nameof(IRestMethodInfoTests.FetchSomeStuffWithDynamicHeaderCollectionAndDynamicHeader))]
    [Arguments(nameof(IRestMethodInfoTests.PostSomeStuffWithDynamicHeaderCollectionAndDynamicHeader))]
    public async Task DynamicHeaderCollectionShouldWorkWithDynamicHeader(string interfaceMethodName)
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == interfaceMethodName));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.AuthorizeParameterInfo).IsNull();
        await Assert.That(fixture.PropertyParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();

        await Assert.That(fixture.HeaderParameterMap).HasSingleItem();
        await Assert.That(fixture.HeaderParameterMap[1]).IsEqualTo("Authorization");
        await Assert.That(fixture.HasHeaderCollection).IsTrue();
        await Assert.That(fixture.HeaderCollectionAt(2)).IsTrue();

        input = typeof(IRestMethodInfoTests);
        fixture = new(
            input,
            input
                .GetMethods()
                .First(
                    x =>
                        x.Name
                        == nameof(
                            IRestMethodInfoTests.FetchSomeStuffWithDynamicHeaderCollectionAndDynamicHeaderOrderFlipped)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.AuthorizeParameterInfo).IsNull();
        await Assert.That(fixture.PropertyParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();

        await Assert.That(fixture.HeaderParameterMap).HasSingleItem();
        await Assert.That(fixture.HeaderParameterMap[2]).IsEqualTo("Authorization");
        await Assert.That(fixture.HasHeaderCollection).IsTrue();
        await Assert.That(fixture.HeaderCollectionAt(1)).IsTrue();
    }

    /// <summary>Verifies a dynamic header collection works alongside a path member in a custom header.</summary>
    /// <param name="interfaceMethodName">The interface method to build the rest method info for.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(nameof(IRestMethodInfoTests.FetchSomeStuffWithPathMemberInCustomHeaderAndDynamicHeaderCollection))]
    [Arguments(nameof(IRestMethodInfoTests.PostSomeStuffWithPathMemberInCustomHeaderAndDynamicHeaderCollection))]
    public async Task DynamicHeaderCollectionShouldWorkWithPathMemberDynamicHeader(
        string interfaceMethodName)
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == interfaceMethodName));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.AuthorizeParameterInfo).IsNull();
        await Assert.That(fixture.PropertyParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();

        await Assert.That(fixture.HeaderParameterMap).HasSingleItem();
        await Assert.That(fixture.HeaderParameterMap[0]).IsEqualTo("X-PathMember");
        await Assert.That(fixture.HasHeaderCollection).IsTrue();
        await Assert.That(fixture.HeaderCollectionAt(1)).IsTrue();
    }

    /// <summary>Verifies a dynamic header collection works when placed in the middle of parameters.</summary>
    /// <param name="interfaceMethodName">The interface method to build the rest method info for.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(nameof(IRestMethodInfoTests.FetchSomeStuffWithHeaderCollection))]
    [Arguments(nameof(IRestMethodInfoTests.PostSomeStuffWithHeaderCollection))]
    public async Task DynamicHeaderCollectionInMiddleOfParamsShouldWork(string interfaceMethodName)
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == interfaceMethodName));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.AuthorizeParameterInfo).IsNull();
        await Assert.That(fixture.PropertyParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();

        await Assert.That(fixture.QueryParameterMap[2]).IsEqualTo("baz");
        await Assert.That(fixture.HasHeaderCollection).IsTrue();
        await Assert.That(fixture.HeaderCollectionAt(1)).IsTrue();
    }

    /// <summary>Verifies only a single dynamic header collection is permitted per method.</summary>
    /// <param name="interfaceMethodName">The interface method to build the rest method info for.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(nameof(IRestMethodInfoTests.FetchSomeStuffWithDuplicateHeaderCollection))]
    [Arguments(nameof(IRestMethodInfoTests.PostSomeStuffWithDuplicateHeaderCollection))]
    public async Task DynamicHeaderCollectionShouldOnlyAllowOne(string interfaceMethodName)
    {
        var input = typeof(IRestMethodInfoTests);

        await Assert.That(
            () =>
                new RestMethodInfoInternal(
                    input,
                    input.GetMethods().First(x => x.Name == interfaceMethodName))).ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies a dynamic header collection works alongside query and array query parameters and a property.</summary>
    /// <param name="interfaceMethodName">The interface method to build the rest method info for.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(nameof(IRestMethodInfoTests.FetchSomeStuffWithHeaderCollectionQueryParamAndArrayQueryParam))]
    [Arguments(nameof(IRestMethodInfoTests.PostSomeStuffWithHeaderCollectionQueryParamAndArrayQueryParam))]
    public async Task DynamicHeaderCollectionShouldWorkWithProperty(string interfaceMethodName)
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == interfaceMethodName));
        await Assert.That(fixture.BodyParameterInfo).IsNull();
        await Assert.That(fixture.AuthorizeParameterInfo).IsNull();

        await Assert.That(fixture.QueryParameterMap.Count).IsEqualTo(2);
        await Assert.That(fixture.QueryParameterMap[1]).IsEqualTo("id");
        await Assert.That(fixture.QueryParameterMap[2]).IsEqualTo("someArray");

        await Assert.That(fixture.PropertyParameterMap).HasSingleItem();

        await Assert.That(fixture.HasHeaderCollection).IsTrue();
        await Assert.That(fixture.HeaderCollectionAt(0)).IsTrue();
    }

    /// <summary>Verifies a dynamic header collection only works with supported parameter semantics.</summary>
    /// <param name="interfaceMethodName">The interface method to build the rest method info for.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(nameof(IRestMethodInfoTests.FetchSomeStuffWithHeaderCollectionOfUnsupportedType))]
    [Arguments(nameof(IRestMethodInfoTests.PostSomeStuffWithHeaderCollectionOfUnsupportedType))]
    public async Task DynamicHeaderCollectionShouldOnlyWorkWithSupportedSemantics(
        string interfaceMethodName)
    {
        var input = typeof(IRestMethodInfoTests);
        await Assert.That(
            () =>
                new RestMethodInfoInternal(
                    input,
                    input.GetMethods().First(x => x.Name == interfaceMethodName))).ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies dynamic request properties are mapped correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DynamicRequestPropertiesShouldWork()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    x =>
                        x.Name
                        == nameof(IRestMethodInfoTests.FetchSomeStuffWithDynamicRequestProperty)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.HeaderParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();

        await Assert.That(fixture.PropertyParameterMap[1]).IsEqualTo("SomeProperty");
    }

    /// <summary>Verifies a dynamic request property works alongside a body.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DynamicRequestPropertyShouldWorkWithBody()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    x =>
                        x.Name
                        == nameof(IRestMethodInfoTests.PostSomeStuffWithDynamicRequestProperty)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.HeaderParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNotNull();
        await Assert.That(fixture.AuthorizeParameterInfo).IsNull();
        await Assert.That(fixture.HasHeaderCollection).IsFalse();

        await Assert.That(fixture.PropertyParameterMap[2]).IsEqualTo("SomeProperty");
    }

    /// <summary>Verifies multiple dynamic request properties work alongside a body.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DynamicRequestPropertiesShouldWorkWithBody()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    x =>
                        x.Name
                        == nameof(
                            IRestMethodInfoTests.PostSomeStuffWithDynamicRequestProperties)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.HeaderParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNotNull();
        await Assert.That(fixture.AuthorizeParameterInfo).IsNull();
        await Assert.That(fixture.HasHeaderCollection).IsFalse();

        await Assert.That(fixture.PropertyParameterMap[2]).IsEqualTo("SomeProperty");
        await Assert.That(fixture.PropertyParameterMap[3]).IsEqualTo("SomeOtherProperty");
    }

    /// <summary>Verifies a dynamic request property works without a body for put, post and patch.</summary>
    /// <param name="interfaceMethodName">The interface method to build the rest method info for.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(nameof(IRestMethodInfoTests.PutSomeStuffWithoutBodyAndWithDynamicRequestProperty))]
    [Arguments(nameof(IRestMethodInfoTests.PostSomeStuffWithoutBodyAndWithDynamicRequestProperty))]
    [Arguments(nameof(IRestMethodInfoTests.PatchSomeStuffWithoutBodyAndWithDynamicRequestProperty))]
    public async Task DynamicRequestPropertyShouldWorkWithoutBody(string interfaceMethodName)
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == interfaceMethodName));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.HeaderParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();
        await Assert.That(fixture.AuthorizeParameterInfo).IsNull();
        await Assert.That(fixture.HasHeaderCollection).IsFalse();

        await Assert.That(fixture.PropertyParameterMap[1]).IsEqualTo("SomeProperty");
    }

    /// <summary>Verifies a dynamic request property works with an inferred body for put, post and patch.</summary>
    /// <param name="interfaceMethodName">The interface method to build the rest method info for.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(nameof(IRestMethodInfoTests.PutSomeStuffWithInferredBodyAndWithDynamicRequestProperty))]
    [Arguments(nameof(IRestMethodInfoTests.PostSomeStuffWithInferredBodyAndWithDynamicRequestProperty))]
    [Arguments(nameof(IRestMethodInfoTests.PatchSomeStuffWithInferredBodyAndWithDynamicRequestProperty))]
    public async Task DynamicRequestPropertyShouldWorkWithInferredBody(string interfaceMethodName)
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == interfaceMethodName));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.HeaderParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNotNull();
        await Assert.That(fixture.AuthorizeParameterInfo).IsNull();
        await Assert.That(fixture.HasHeaderCollection).IsFalse();

        await Assert.That(fixture.PropertyParameterMap[1]).IsEqualTo("SomeProperty");
        await Assert.That(fixture.BodyParameterInfo!.Item3).IsEqualTo(2);
    }

    /// <summary>Verifies dynamic request properties without keys default the key to the parameter name.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DynamicRequestPropertiesWithoutKeysShouldDefaultKeyToParameterName()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    x =>
                        x.Name
                        == nameof(
                            IRestMethodInfoTests.FetchSomeStuffWithDynamicRequestPropertyWithoutKey)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.HeaderParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();

        await Assert.That(fixture.PropertyParameterMap[1]).IsEqualTo("someValue");
        await Assert.That(fixture.PropertyParameterMap[2]).IsEqualTo("someOtherValue");
    }

    /// <summary>Verifies dynamic request properties with duplicate keys do not throw.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DynamicRequestPropertiesWithDuplicateKeysDontBlowUp()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    x =>
                        x.Name
                        == nameof(
                            IRestMethodInfoTests.FetchSomeStuffWithDynamicRequestPropertyWithDuplicateKey)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.HeaderParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo).IsNull();

        await Assert.That(fixture.PropertyParameterMap[1]).IsEqualTo("SomeProperty");
        await Assert.That(fixture.PropertyParameterMap[2]).IsEqualTo("SomeProperty");
    }

    /// <summary>Verifies value-type body parameters do not throw when buffered.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ValueTypesDontBlowUpBuffered()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(x => x.Name == nameof(IRestMethodInfoTests.OhYeahValueTypes)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo!.Item1).IsEqualTo(BodySerializationMethod.Default);
        await Assert.That(fixture.BodyParameterInfo.Item2).IsTrue(); // buffered default
        await Assert.That(fixture.BodyParameterInfo.Item3).IsEqualTo(1);

        await Assert.That(fixture.ReturnResultType).IsEqualTo(typeof(bool));
    }

    /// <summary>Verifies value-type body parameters do not throw when unbuffered.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ValueTypesDontBlowUpUnBuffered()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(x => x.Name == nameof(IRestMethodInfoTests.OhYeahValueTypesUnbuffered)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo!.Item1).IsEqualTo(BodySerializationMethod.Default);
        await Assert.That(fixture.BodyParameterInfo.Item2).IsFalse(); // unbuffered specified
        await Assert.That(fixture.BodyParameterInfo.Item3).IsEqualTo(1);

        await Assert.That(fixture.ReturnResultType).IsEqualTo(typeof(bool));
    }

    /// <summary>Verifies a stream pull method parses its body parameter correctly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task StreamMethodPullWorks()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(x => x.Name == nameof(IRestMethodInfoTests.PullStreamMethod)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);
        await Assert.That(fixture.QueryParameterMap).IsEmpty();
        await Assert.That(fixture.BodyParameterInfo!.Item1).IsEqualTo(BodySerializationMethod.Default);
        await Assert.That(fixture.BodyParameterInfo.Item2).IsTrue();
        await Assert.That(fixture.BodyParameterInfo.Item3).IsEqualTo(1);

        await Assert.That(fixture.ReturnResultType).IsEqualTo(typeof(bool));
    }

    /// <summary>Verifies a method returning a non-generic task resolves its return type.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ReturningTaskShouldWork()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.VoidPost)));
        await Assert.That(fixture.ParameterMap[0].Name).IsEqualTo("id");
        await Assert.That(fixture.ParameterMap[0].Type).IsEqualTo(ParameterType.Normal);

        await Assert.That(fixture.ReturnType).IsEqualTo(typeof(Task));
        await Assert.That(fixture.ReturnResultType).IsEqualTo(typeof(void));
    }

    /// <summary>Verifies a synchronous method throws an argument exception.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SyncMethodsShouldThrow()
    {
        var shouldDie = true;

        try
        {
            var input = typeof(IRestMethodInfoTests);
            _ = new RestMethodInfoInternal(
                input,
                input
                    .GetMethods()
                    .First(x => x.Name == nameof(IRestMethodInfoTests.AsyncOnlyBuddy)));
        }
        catch (ArgumentException)
        {
            shouldDie = false;
        }

        await Assert.That(shouldDie).IsFalse();
    }

    /// <summary>Verifies the patch attribute sets the HTTP method to PATCH.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UsingThePatchAttributeSetsTheCorrectMethod()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == nameof(IRestMethodInfoTests.PatchSomething)));

        await Assert.That(fixture.HttpMethod.Method).IsEqualTo("PATCH");
    }

    /// <summary>Verifies the options attribute sets the HTTP method to OPTIONS.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UsingOptionsAttribute()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(x => x.Name == nameof(IDummyHttpApi.SendOptions)));

        await Assert.That(fixture.HttpMethod.Method).IsEqualTo("OPTIONS");
    }

    /// <summary>Verifies the api response flag is set for a method returning an API response.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ApiResponseShouldBeSet()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(x => x.Name == nameof(IRestMethodInfoTests.PostReturnsApiResponse)));

        await Assert.That(fixture.IsApiResponse).IsTrue();
    }

    /// <summary>Verifies the api response flag is not set for a method returning a non-API response.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ApiResponseShouldNotBeSet()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(x => x.Name == nameof(IRestMethodInfoTests.PostReturnsNonApiResponse)));

        await Assert.That(fixture.IsApiResponse).IsFalse();
    }

    /// <summary>Verifies parameter mapping with a header, query parameter and array query parameter.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ParameterMappingWithHeaderQueryParamAndQueryArrayParam()
    {
        var input = typeof(IRestMethodInfoTests);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods()
                .First(
                    x =>
                        x.Name
                        == nameof(
                            IRestMethodInfoTests.FetchSomeStuffWithDynamicHeaderQueryParamAndArrayQueryParam)));

        await Assert.That(fixture.HttpMethod.Method).IsEqualTo("GET");
        await Assert.That(fixture.QueryParameterMap.Count).IsEqualTo(2);
        await Assert.That(fixture.HeaderParameterMap).HasSingleItem();
        await Assert.That(fixture.PropertyParameterMap).HasSingleItem();
    }

    /// <summary>Verifies a generic return type that is not a task or observable throws.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task GenericReturnTypeIsNotTaskOrObservableShouldThrow()
    {
        var input = typeof(IRestMethodInfoTests);
        await Assert.That(
            () =>
                new RestMethodInfoInternal(
                    input,
                    input
                        .GetMethods()
                        .First(
                            x => x.Name == nameof(IRestMethodInfoTests.InvalidGenericReturnType)))).ThrowsExactly<ArgumentException>();
    }

    /// <summary>Verifies an internal sync generic return type sets the deserialized type to the return type.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task InternalSyncGenericReturnTypeSetsDeserializedTypeToReturnType()
    {
        var input = typeof(IInternalSyncGenericReturnTypeApi);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .First(x => x.Name == nameof(IInternalSyncGenericReturnTypeApi.GetValues)));

        await Assert.That(fixture.ReturnType).IsEqualTo(typeof(List<string>));
        await Assert.That(fixture.ReturnResultType).IsEqualTo(typeof(List<string>));
        await Assert.That(fixture.DeserializedResultType).IsEqualTo(typeof(List<string>));
    }

    /// <summary>Verifies an internal sync IApiResponse generic return type sets the deserialized type to the generic argument.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task InternalSyncIApiResponseGenericReturnTypeSetsDeserializedTypeToGenericArgument()
    {
        var input = typeof(IInternalSyncGenericApiResponseReturnTypeApi);
        var fixture = new RestMethodInfoInternal(
            input,
            input
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .First(x => x.Name == nameof(IInternalSyncGenericApiResponseReturnTypeApi.GetResponse)));

        await Assert.That(fixture.ReturnType).IsEqualTo(typeof(IApiResponse<string>));
        await Assert.That(fixture.ReturnResultType).IsEqualTo(typeof(IApiResponse<string>));
        await Assert.That(fixture.DeserializedResultType).IsEqualTo(typeof(string));
    }
}
