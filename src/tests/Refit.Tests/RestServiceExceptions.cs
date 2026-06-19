// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>Tests that <see cref="RestService"/> throws for interfaces with invalid Refit definitions.</summary>
[RequiresUnreferencedCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
[RequiresDynamicCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
public class RestServiceExceptions
{
    /// <summary>Verifies that declaring multiple cancellation tokens throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ManyCancellationTokensShouldThrow()
    {
        var exception = await Assert.That(() => RestService.For<IManyCancellationTokens>("https://api.github.com")).ThrowsExactly<ArgumentException>();
        await AssertExceptionContains("only contain a single CancellationToken", exception!);
    }

    /// <summary>Verifies that declaring multiple header collections throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ManyHeaderCollectionShouldThrow()
    {
        var exception = await Assert.That(() => RestService.For<IManyHeaderCollections>("https://api.github.com")).ThrowsExactly<ArgumentException>();
        await AssertExceptionContains("Only one parameter can be a HeaderCollection parameter", exception!);
    }

    /// <summary>Verifies that a header collection with an unsupported value type throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InvalidHeaderCollectionTypeShouldThrow()
    {
        var exception = await Assert.That(() => RestService.For<IHeaderCollectionWrongType>("https://api.github.com")).ThrowsExactly<ArgumentException>();
        await AssertExceptionContains("HeaderCollection parameter of type", exception!);
    }

    /// <summary>Verifies that a URL not starting with a slash throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UrlDoesntStartWithSlashShouldThrow()
    {
        var exception = await Assert.That(() => RestService.For<IDoesNotStartSlash>("https://api.github.com")).ThrowsExactly<ArgumentException>();
        await AssertExceptionContains("must start with '/' and be of the form", exception!);
    }

    /// <summary>Verifies that a URL containing CR or LF characters throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UrlContainsCrlfShouldThrow()
    {
        var exception = await Assert.That(() => RestService.For<IUrlContainsCrlf>("https://api.github.com")).ThrowsExactly<ArgumentException>();
        await AssertExceptionContains("must not contain CR or LF characters", exception!);
    }

    /// <summary>Verifies that a non-string round-tripping parameter throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RoundTripParameterNotStringShouldThrow()
    {
        var exception = await Assert.That(() => RestService.For<IRoundTripNotString>("https://api.github.com")).ThrowsExactly<ArgumentException>();
        await AssertExceptionContains("has round-tripping parameter", exception!);
    }

    /// <summary>Verifies that a round-tripping parameter name with leading whitespace throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RoundTripWithLeadingWhitespaceShouldThrow()
    {
        var exception = await Assert.That(() => RestService.For<IRoundTrippingLeadingWhitespace>("https://api.github.com")).ThrowsExactly<ArgumentException>();
        await AssertExceptionContains("has parameter  **path, but no method parameter matches", exception!);
    }

    /// <summary>Verifies that a round-tripping parameter name with trailing whitespace throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RoundTripWithTrailingWhitespaceShouldThrow()
    {
        var exception = await Assert.That(() => RestService.For<IRoundTrippingTrailingWhitespace>("https://api.github.com")).ThrowsExactly<ArgumentException>();
        await AssertExceptionContains("has parameter ** path, but no method parameter matches", exception!);
    }

    /// <summary>Verifies that an invalid parameter substitution token throws when invoked.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InvalidParamSubstitutionShouldThrow()
    {
        var service = RestService.For<IInvalidParamSubstitution>("https://api.github.com");
        await Assert.That(service).IsNotNull();

        await Assert.That(() => (Task)service.GetValue("throws")).ThrowsExactly<ApiException>();
    }

    /// <summary>Verifies that an invalid fragment parameter substitution token throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InvalidFragmentParamSubstitutionShouldThrow()
    {
        var exception = await Assert.That(() => RestService.For<IInvalidFragmentParamSubstitution>("https://api.github.com")).ThrowsExactly<ArgumentException>();
        await AssertExceptionContains("but no method parameter matches", exception!);
    }

    /// <summary>Verifies that a URL parameter with no matching method argument throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task UrlNoMatchingParameterShouldThrow()
    {
        var exception = await Assert.That(() => RestService.For<IUrlNoMatchingParameters>("https://api.github.com")).ThrowsExactly<ArgumentException>();
        await AssertExceptionContains("but no method parameter matches", exception!);
    }

    /// <summary>Verifies that combining multipart with a body parameter throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartAndBodyShouldThrow()
    {
        var exception = await Assert.That(() => RestService.For<IMultipartAndBody>("https://api.github.com")).ThrowsExactly<ArgumentException>();
        await AssertExceptionContains("Multipart requests may not contain a Body parameter", exception!);
    }

    /// <summary>Verifies that declaring multiple body parameters throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ManyBodyShouldThrow()
    {
        var exception = await Assert.That(() => RestService.For<IManyBody>("https://api.github.com")).ThrowsExactly<ArgumentException>();
        await AssertExceptionContains("Only one parameter can be a Body parameter", exception!);
    }

    /// <summary>Verifies that multiple complex-type parameters without a body marker throw.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ManyComplexTypesShouldThrow()
    {
        var exception = await Assert.That(() => RestService.For<IManyComplexTypes>("https://api.github.com")).ThrowsExactly<ArgumentException>();
        await AssertExceptionContains("Multiple complex types found. Specify one parameter as the body using BodyAttribute", exception!);
    }

    /// <summary>Verifies that declaring multiple authorize parameters throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ManyAuthorizeAttributesShouldThrow()
    {
        var exception = await Assert.That(() => RestService.For<IManyAuthorize>("https://api.github.com")).ThrowsExactly<ArgumentException>();
        await AssertExceptionContains("Only one parameter can be an Authorize parameter", exception!);
    }

    /// <summary>Verifies that an unsupported synchronous return type throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InvalidReturnTypeShouldThrow()
    {
        var exception = await Assert.That(() => RestService.For<IInvalidReturnType>("https://api.github.com")).ThrowsExactly<ArgumentException>();
        await AssertExceptionContains("is invalid. All REST Methods must return either Task<T> or ValueTask<T> or IObservable<T>", exception!);
    }

    /// <summary>Verifies that a raw non-generic IApiResponse return type throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InvalidRawApiResponseReturnTypeShouldThrow()
    {
        var exception = await Assert.That(() => RestService.For<IInvalidReturnTypeIApiResponse>("https://api.github.com")).ThrowsExactly<ArgumentException>();
        await AssertExceptionContains("is invalid. All REST Methods must return either Task<T> or ValueTask<T> or IObservable<T>", exception!);
    }

    /// <summary>Asserts that the exception message contains the expected substring.</summary>
    /// <param name="expectedSubstring">The substring expected within the exception message.</param>
    /// <param name="exception">The exception whose message is inspected.</param>
    /// <returns>A task that completes when the assertion has run.</returns>
    private static async Task AssertExceptionContains(string expectedSubstring, Exception exception)
    {
        await Assert.That(exception.Message!).Contains(expectedSubstring, StringComparison.Ordinal);
    }
}
