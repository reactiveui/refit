// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Linq;

namespace Refit.Reflection.Tests;

/// <summary>Pins that the reflection request builder flattens a <see cref="FormObjectAttribute"/> parameter into one part
/// per property while a plain parameter stays a single multipart part.</summary>
public sealed class ReflectionMultipartFormObjectTests
{
    /// <summary>The multipart parameter index of the plain text part.</summary>
    private const int TitlePartIndex = 0;

    /// <summary>The multipart parameter index of the flattened form-object part.</summary>
    private const int AddressPartIndex = 1;

    /// <summary>The number of parts a two-property form object flattens into.</summary>
    private const int FlattenedAddressPartCount = 2;

    /// <summary>Verifies a plain parameter yields a single part while a form-object parameter yields one part per property.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task FormObjectParameterFlattensToOnePartPerProperty()
    {
        var settings = new RefitSettings();
        var builder = new RequestBuilderImplementation(typeof(IReflectionMultipartFormObjectApi), settings);
        var method = new RestMethodInfoInternal(
            typeof(IReflectionMultipartFormObjectApi),
            typeof(IReflectionMultipartFormObjectApi).GetMethod(nameof(IReflectionMultipartFormObjectApi.Upload))!,
            settings);

        using var titleContent = new MultipartFormDataContent();
        builder.AddMultiPart(method, TitlePartIndex, "quarterly report", titleContent);

        using var addressContent = new MultipartFormDataContent();
        builder.AddMultiPart(
            method,
            AddressPartIndex,
            new MultipartAddress { City = "Springfield", PostalCode = "12345" },
            addressContent);

        await Assert.That(titleContent.Count()).IsEqualTo(1);
        await Assert.That(addressContent.Count()).IsEqualTo(FlattenedAddressPartCount);
    }
}
