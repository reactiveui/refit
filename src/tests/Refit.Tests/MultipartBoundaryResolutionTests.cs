// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>
/// Verifies how <see cref="RestMethodInfoInternal"/> resolves the multipart boundary for a method:
/// an explicit boundary, the framework default boundary, and the empty boundary of a non-multipart method.
/// </summary>
public sealed class MultipartBoundaryResolutionTests
{
    /// <summary>The framework default multipart boundary applied when a method declares <c>[Multipart]</c> with no argument.</summary>
    private const string DefaultBoundary = "----MyGreatBoundary";

    /// <summary>The explicit boundary declared by <see cref="IRunscopeApi.UploadStreamWithCustomBoundary"/>.</summary>
    private const string CustomBoundary = "-----SomeCustomBoundary";

    /// <summary>A method declaring an explicit boundary uses that boundary verbatim.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ExplicitBoundaryIsTakenFromAttribute()
    {
        var input = typeof(IRunscopeApi);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(static x => x.Name == nameof(IRunscopeApi.UploadStreamWithCustomBoundary)));

        await Assert.That(fixture.IsMultipart).IsTrue();
        await Assert.That(fixture.MultipartBoundary).IsEqualTo(CustomBoundary);
    }

    /// <summary>A method declaring <c>[Multipart]</c> without an argument uses the framework default boundary.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DefaultBoundaryUsesFrameworkDefault()
    {
        var input = typeof(IRunscopeApi);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(static x => x.Name == nameof(IRunscopeApi.UploadStream)));

        await Assert.That(fixture.IsMultipart).IsTrue();
        await Assert.That(fixture.MultipartBoundary).IsEqualTo(DefaultBoundary);
    }

    /// <summary>A non-multipart method has an empty boundary.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task NonMultipartMethodHasEmptyBoundary()
    {
        var input = typeof(IDummyHttpApi);
        var fixture = new RestMethodInfoInternal(
            input,
            input.GetMethods().First(static x => x.Name == nameof(IDummyHttpApi.FetchSomeStuff)));

        await Assert.That(fixture.IsMultipart).IsFalse();
        await Assert.That(fixture.MultipartBoundary).IsEqualTo(string.Empty);
    }
}
