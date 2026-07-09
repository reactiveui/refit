// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Response error-handling tests that exercise the Newtonsoft content serializer.</summary>
public sealed class ResponseNewtonsoftTests
{
    /// <summary>Refit service used to exercise alias and response handling.</summary>
    public interface IMyAliasService
    {
        /// <summary>Gets the aliased test object from the test endpoint.</summary>
        /// <returns>The deserialized <see cref="TestAliasObject"/>.</returns>
        [Get("/aliasTest")]
        Task<TestAliasObject?> GetTestObject();

        /// <summary>Gets the test object wrapped in an <see cref="ApiResponse{T}"/>.</summary>
        /// <returns>The wrapped response.</returns>
        [Get("/GetApiResponseTestObject")]
        Task<ApiResponse<TestAliasObject>?> GetApiResponseTestObject();
    }

    /// <summary>Verifies that a non-JSON response surfaces as an ApiException with the Newtonsoft serializer.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WithNonJsonResponseUsingNewtonsoftJsonContentSerializer_ShouldReturnApiException()
    {
        const string nonJsonResponse = "bad response";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(nonJsonResponse)
        };
        expectedResponse.Content.Headers.Clear();

        var handler = new StubHttp
        {
            {
                Route.Get("http://api/aliasTest"),
                Reply.From(req => expectedResponse)
            },
        };

        var newtonSoftFixture = handler.CreateClient<IMyAliasService>("http://api", new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer()
        });

        var actualException = await Assert.That(newtonSoftFixture.GetTestObject).ThrowsExactly<ApiException>();

        await Assert.That(actualException!.InnerException).IsTypeOf<JsonReaderException>();
        await Assert.That(actualException.Content).IsNotNull();
        await Assert.That(actualException.Content).IsEqualTo(nonJsonResponse);
    }

    /// <summary>Verifies that a non-JSON response surfaces through the ApiResponse error with the Newtonsoft serializer.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task WithNonJsonResponseUsingNewtonsoftJsonContentSerializer_ShouldReturnApiResponse()
    {
        const string nonJsonResponse = "bad response";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(nonJsonResponse)
        };
        expectedResponse.Content.Headers.Clear();

        var handler = new StubHttp
        {
            {
                Route.Get($"http://api/{nameof(IMyAliasService.GetApiResponseTestObject)}"),
                Reply.From(req => expectedResponse)
            },
        };

        var newtonSoftFixture = handler.CreateClient<IMyAliasService>("http://api", new RefitSettings
        {
            ContentSerializer = new NewtonsoftJsonContentSerializer()
        });

        var apiResponse = await newtonSoftFixture.GetApiResponseTestObject();

        await Assert.That(apiResponse!.Error).IsNotNull();
        await Assert.That(apiResponse.Error!.InnerException).IsTypeOf<JsonReaderException>();
        await Assert.That(apiResponse.HasResponseError(out var error)).IsTrue();
        await Assert.That(error!.Content).IsNotNull();
        await Assert.That(error.Content).IsEqualTo(nonJsonResponse);
    }
}
