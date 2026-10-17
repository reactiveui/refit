// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Refit.Tests;

/// <summary>Verifies query and form naming in generated and reflection clients.</summary>
public sealed class QueryAttributeNameTests
{
    /// <summary>The first scalar value.</summary>
    private const string FirstValue = "value";

    /// <summary>The second scalar value.</summary>
    private const string SecondValue = "second";

    /// <summary>The second collection element.</summary>
    private const int SecondId = 2;

    /// <summary>Checks explicit query names, alias precedence and collection expansion.</summary>
    /// <param name="method">The API method.</param>
    /// <param name="expected">The expected path and query.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the test.</returns>
    [Test]
    [Arguments(nameof(IQueryAttributeNameApi.Scalar), "/query?client_id=value")]
    [Arguments(nameof(IQueryAttributeNameApi.Alias), "/query?chosen=value")]
    [Arguments(nameof(IQueryAttributeNameApi.Collection), "/query?ids=1&ids=2")]
    [Arguments(nameof(IQueryAttributeNameApi.Object), "/query?custom=value&alias=second&scope.nested=3")]
    [Arguments(nameof(IQueryAttributeNameApi.Path), "/value?filter=second")]
    public async Task Query_NameHonoredByBothBuilders(string method, string expected, CancellationToken cancellationToken)
    {
        using var client = HttpClientTestFactory.Create(new TestHttpMessageHandler(), new("https://api/"));
        var api = RestService.ForGenerated<IQueryAttributeNameApi>(client, new());
        var data = new NamedQueryData();
        object[] arguments = method switch
        {
            nameof(IQueryAttributeNameApi.Collection) => [(int[])[1, SecondId]],
            nameof(IQueryAttributeNameApi.Object) => [data],
            nameof(IQueryAttributeNameApi.Path) => [FirstValue, SecondValue],
            _ => [FirstValue]
        };
        using var reflected = await new RequestBuilderImplementation<IQueryAttributeNameApi>()
            .BuildRequestFactoryForMethod(method)(arguments).WaitAsync(cancellationToken);
        using var generated = await (method switch
        {
            nameof(IQueryAttributeNameApi.Scalar) => api.Scalar(FirstValue),
            nameof(IQueryAttributeNameApi.Alias) => api.Alias(FirstValue),
            nameof(IQueryAttributeNameApi.Collection) => api.Collection([1, SecondId]),
            nameof(IQueryAttributeNameApi.Object) => api.Object(data),
            _ => api.Path(FirstValue, SecondValue)
        }).WaitAsync(cancellationToken);
        await Assert.That(generated.RequestUri!.OriginalString).IsEqualTo(expected);
        await Assert.That(reflected.RequestUri!.OriginalString).IsEqualTo(expected);
    }

    /// <summary>Checks explicit form names and AliasAs precedence.</summary>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task Form_NameHonoredByBothBuilders(CancellationToken cancellationToken)
    {
        using var client = HttpClientTestFactory.Create(new TestHttpMessageHandler(), new("https://api/"));
        var api = RestService.ForGenerated<IQueryAttributeNameApi>(client, new());
        var data = new NamedQueryData();
        using var generated = await api.Form(data).WaitAsync(cancellationToken);
        using var reflected = await new RequestBuilderImplementation<IQueryAttributeNameApi>()
            .BuildRequestFactoryForMethod(nameof(IQueryAttributeNameApi.Form))([data]).WaitAsync(cancellationToken);
        const string expected = "custom=value&alias=second&scope.nested=3";
        await Assert.That(await generated.Content!.ReadAsStringAsync(cancellationToken)).IsEqualTo(expected);
        await Assert.That(await reflected.Content!.ReadAsStringAsync(cancellationToken)).IsEqualTo(expected);
    }

    /// <summary>A DTO with explicit query names and competing serializer names.</summary>
    public sealed class NamedQueryData
    {
        /// <summary>The prefixed property value.</summary>
        private const int PrefixedValue = 3;

        /// <summary>Gets the explicitly named value.</summary>
        [Query(Name = "custom")]
        [JsonPropertyName("json")]
        public string Value { get; init; } = FirstValue;

        /// <summary>Gets the value whose AliasAs name wins.</summary>
        [Query(Name = "ignored")]
        [AliasAs("alias")]
        public string Aliased { get; init; } = SecondValue;

        /// <summary>Gets the value whose explicit name is prefixed.</summary>
        [Query(".", "scope", Name = "nested")]
        public int Prefixed { get; init; } = PrefixedValue;
    }
}
