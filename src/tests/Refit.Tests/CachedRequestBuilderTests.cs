// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Reflection;

namespace Refit.Tests;

/// <summary>Tests for the cached request builder implementation.</summary>
public class CachedRequestBuilderTests
{
    /// <summary>Expected cache entry and build count after two distinct method invocations.</summary>
    private const int ExpectedTwoEntries = 2;

    /// <summary>Expected cache entry and build count after three distinct method invocations.</summary>
    private const int ExpectedThreeEntries = 3;

    /// <summary>Verifies the cached builder throws when constructed with a null inner builder.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CachedBuilder_ThrowsForNullInnerBuilder() => await Assert.That(() => new CachedRequestBuilderImplementation(null!)).ThrowsExactly<ArgumentNullException>();

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
        var innerBuilder = new CountingRequestBuilder();
        var requestBuilder = new CachedRequestBuilderImplementation(innerBuilder);

        _ = requestBuilder.BuildRestResultFuncForMethod(nameof(IGeneralRequests.SingleParameter), [typeof(string)]);
        await Assert.That(requestBuilder.MethodDictionary).HasSingleItem();
        await Assert.That(innerBuilder.BuildCount).IsEqualTo(1);

        _ = requestBuilder.BuildRestResultFuncForMethod(nameof(IGeneralRequests.MultiParameter), [typeof(string), typeof(string)]);
        await Assert.That(requestBuilder.MethodDictionary.Count).IsEqualTo(ExpectedTwoEntries);
        await Assert.That(innerBuilder.BuildCount).IsEqualTo(ExpectedTwoEntries);

        _ = requestBuilder.BuildRestResultFuncForMethod(
            nameof(IGeneralRequests.SingleGenericMultiParameter),
            [typeof(string), typeof(string), typeof(string)],
            [typeof(string)]);
        await Assert.That(requestBuilder.MethodDictionary.Count).IsEqualTo(ExpectedThreeEntries);
        await Assert.That(innerBuilder.BuildCount).IsEqualTo(ExpectedThreeEntries);
    }

    /// <summary>Verifies repeated identical requests do not create duplicate cache entries.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NoDuplicateEntriesTest()
    {
        var innerBuilder = new CountingRequestBuilder();
        var requestBuilder = new CachedRequestBuilderImplementation(innerBuilder);

        _ = requestBuilder.BuildRestResultFuncForMethod(nameof(IGeneralRequests.SingleParameter), [typeof(string)]);
        await Assert.That(requestBuilder.MethodDictionary).HasSingleItem();

        _ = requestBuilder.BuildRestResultFuncForMethod(nameof(IGeneralRequests.SingleParameter), [typeof(string)]);
        await Assert.That(requestBuilder.MethodDictionary).HasSingleItem();

        _ = requestBuilder.BuildRestResultFuncForMethod(nameof(IGeneralRequests.SingleParameter), [typeof(string)]);
        await Assert.That(requestBuilder.MethodDictionary).HasSingleItem();
        await Assert.That(innerBuilder.BuildCount).IsEqualTo(1);
    }

    /// <summary>Verifies same-named overloads with different parameter types produce distinct cache entries.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SameNameDuplicateEntriesTest()
    {
        var innerBuilder = new CountingRequestBuilder();
        var requestBuilder = new CachedRequestBuilderImplementation(innerBuilder);

        _ = requestBuilder.BuildRestResultFuncForMethod(nameof(IDuplicateNames.SingleParameter), [typeof(string)]);
        await Assert.That(requestBuilder.MethodDictionary).HasSingleItem();

        _ = requestBuilder.BuildRestResultFuncForMethod(nameof(IDuplicateNames.SingleParameter), [typeof(int)]);
        await Assert.That(requestBuilder.MethodDictionary.Count).IsEqualTo(ExpectedTwoEntries);
        await Assert.That(innerBuilder.BuildCount).IsEqualTo(ExpectedTwoEntries);
    }

    /// <summary>Request-builder test double that records how many functions the cache asks it to build.</summary>
    private sealed class CountingRequestBuilder : IRequestBuilder
    {
        /// <summary>Gets the number of build calls received.</summary>
        public int BuildCount { get; private set; }

        /// <inheritdoc />
        public RefitSettings Settings { get; } = new(new NullContentSerializer());

        /// <inheritdoc />
        public Func<HttpClient, object[], object?> BuildRestResultFuncForMethod(
            string methodName,
            Type[]? parameterTypes = null,
            Type[]? genericArgumentTypes = null)
        {
            BuildCount++;
            return static (_, _) => null;
        }
    }

    /// <summary>Content serializer test double used only to satisfy <see cref="RefitSettings"/> construction.</summary>
    private sealed class NullContentSerializer : IHttpContentSerializer
    {
        /// <inheritdoc />
        public HttpContent ToHttpContent<T>(T item) => new ByteArrayContent([]);

        /// <inheritdoc />
        [SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "The method implements IHttpContentSerializer and must preserve the interface shape.")]
        public Task<T?> FromHttpContentAsync<T>(
            HttpContent content,
            CancellationToken cancellationToken = default) => Task.FromResult<T?>(default);

        /// <inheritdoc />
        public string? GetFieldNameForProperty(PropertyInfo propertyInfo) => propertyInfo.Name;
    }
}
