// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>Tests covering opt-in <see cref="FormObjectAttribute"/> flattening of a complex object into form-data parts.</summary>
public partial class MultipartTests
{
    /// <summary>Verifies an opt-in <see cref="FormObjectAttribute"/> model is flattened into one text form-data part per
    /// property, named per property and independent of the content serializer used for other parts.</summary>
    /// <param name="contentSerializerType">The serializer type configured on the request.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(typeof(SystemTextJsonContentSerializer))]
    [Arguments(typeof(XmlContentSerializer))]
    public async Task MultipartFormObjectFlattensEachPropertyIntoOwnTextPart(Type contentSerializerType)
    {
        if (Activator.CreateInstance(contentSerializerType) is not IHttpContentSerializer serializer)
        {
            throw new ArgumentException(
                $"{contentSerializerType.FullName} does not implement {nameof(IHttpContentSerializer)}");
        }

        var model = new FormObjectUploadModel
        {
            Name = FormObjectFullName,
            Age = FormObjectAge,
            Roles = [FormObjectAdminRole, "user"]
        };

        var handler = new MockHttpMessageHandler
        {
            Asserts = static async content =>
            {
                var parts = content.ToList();

                const int expectedPartCount = 3;
                await Assert.That(parts.Count).IsEqualTo(expectedPartCount);

                await AssertTextPart(parts, "full_name", FormObjectFullName);
                await AssertTextPart(parts, "Age", FormObjectAgeText);

                // A collection property is joined by the settings collection format (comma by default), not repeated.
                await AssertTextPart(parts, "Roles", "admin,user");
            }
        };

        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
            ContentSerializer = serializer
        };

        var fixture = RestService.For<IMultipartFormObjectApi>(BaseAddress, settings);
        await fixture.UploadFormObject(model);
    }

    /// <summary>Verifies a nested object is flattened into composed <c>parent.child</c> form fields.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartFormObjectFlattensNestedObjectIntoComposedFields()
    {
        var model = new NestedFormObjectUploadModel
        {
            Title = "Notebook",
            Author = new()
            {
                Name = FormObjectFullName,
                Age = FormObjectAge,
                Roles = [FormObjectAdminRole]
            }
        };

        var handler = new MockHttpMessageHandler
        {
            Asserts = static async content =>
            {
                var parts = content.ToList();

                const int expectedPartCount = 4;
                await Assert.That(parts.Count).IsEqualTo(expectedPartCount);

                await AssertTextPart(parts, "Title", "Notebook");
                await AssertTextPart(parts, "Author.full_name", FormObjectFullName);
                await AssertTextPart(parts, "Author.Age", FormObjectAgeText);
                await AssertTextPart(parts, "Author.Roles", FormObjectAdminRole);
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        var fixture = RestService.For<IMultipartFormObjectApi>(BaseAddress, settings);
        await fixture.UploadNestedFormObject(model);
    }

    /// <summary>Verifies a flattened property's field name is resolved by the content serializer.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartFormObjectResolvesFieldNameFromContentSerializer()
    {
        var handler = new MockHttpMessageHandler
        {
            Asserts = static async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts).HasSingleItem();
                await AssertTextPart(parts, "user_id", "abc-123");
            }
        };

        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
            ContentSerializer = new SystemTextJsonContentSerializer()
        };

        var fixture = RestService.For<IMultipartFormObjectApi>(BaseAddress, settings);
        await fixture.UploadSerializerNamedFormObject(new() { Id = "abc-123" });
    }

    /// <summary>Verifies a flattened model coexists with a separate file part, which stays a file part.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartFormObjectKeepsSeparateFilePartIntact()
    {
        var model = new FormObjectUploadModel { Name = FormObjectFullName, Age = FormObjectAge };

        var handler = new MockHttpMessageHandler
        {
            Asserts = static async content =>
            {
                var parts = content.ToList();

                const int expectedPartCount = 3;
                await Assert.That(parts.Count).IsEqualTo(expectedPartCount);

                await AssertTextPart(parts, "full_name", FormObjectFullName);
                await AssertTextPart(parts, "Age", FormObjectAgeText);

                var filePart = parts.Single(static p => p.Headers.ContentDisposition!.Name == "recipe");
                await Assert.That(filePart.Headers.ContentDisposition!.FileName).IsEqualTo(StreamPartFileName);
                await Assert.That(filePart.Headers.ContentType!.MediaType).IsEqualTo(PdfMediaType);
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        await using var stream = GetTestFileStream(TestFilePath);
        var fixture = RestService.For<IMultipartFormObjectApi>(BaseAddress, settings);
        await fixture.UploadFormObjectWithFile(model, new(stream, StreamPartFileName, PdfMediaType));
    }

    /// <summary>Verifies a flattened dictionary drops an entry whose field name is blank rather than throwing.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartFormObjectSkipsFieldWithBlankName()
    {
        var fields = new Dictionary<string, string>
        {
            ["   "] = "dropped",
            ["kept"] = "value"
        };

        var handler = new MockHttpMessageHandler
        {
            Asserts = static async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts).HasSingleItem();
                await AssertTextPart(parts, "kept", "value");
            }
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };

        var fixture = RestService.For<IMultipartFormObjectApi>(BaseAddress, settings);
        await fixture.UploadFormDictionary(fields);
    }

    /// <summary>Verifies a flattened field whose formatter returns null is sent as an empty part rather than throwing.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MultipartFormObjectRendersNullFormattedValueAsEmptyPart()
    {
        var model = new FormObjectUploadModel { Name = "ignored", Age = 1 };

        var handler = new MockHttpMessageHandler
        {
            Asserts = static async content =>
            {
                var parts = content.ToList();

                await Assert.That(parts.Count).IsGreaterThan(0);
                foreach (var part in parts)
                {
                    await Assert.That(await part.ReadAsStringAsync()).IsEqualTo(string.Empty);
                }
            }
        };

        var settings = new RefitSettings
        {
            HttpMessageHandlerFactory = () => handler,
            FormUrlEncodedParameterFormatter = new NullReturningFormUrlEncodedParameterFormatter()
        };

        var fixture = RestService.For<IMultipartFormObjectApi>(BaseAddress, settings);
        await fixture.UploadFormObject(model);
    }

    /// <summary>Asserts a flattened form field is a plain-text multipart part with the expected name and value.</summary>
    /// <param name="parts">The multipart parts observed by the handler.</param>
    /// <param name="name">The expected content-disposition field name.</param>
    /// <param name="value">The expected plain-text value.</param>
    /// <returns>A task representing the assertion work.</returns>
    private static async Task AssertTextPart(List<HttpContent> parts, string name, string value)
    {
        var part = parts.Single(p => p.Headers.ContentDisposition!.Name == name);

        await Assert.That(part.Headers.ContentDisposition!.FileName).IsNull();
        await Assert.That(part.Headers.ContentType!.MediaType).IsEqualTo(PlainTextMediaType);
        await Assert.That(part.Headers.ContentType!.CharSet).IsEqualTo(Utf8CharSet);
        await Assert.That(await part.ReadAsStringAsync()).IsEqualTo(value);
    }
}
