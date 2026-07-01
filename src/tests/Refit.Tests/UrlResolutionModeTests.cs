// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Tests for <see cref="UrlResolutionMode.Rfc3986"/> base-address resolution.</summary>
public class UrlResolutionModeTests
{
    /// <summary>The RFC 3986 base address used across the resolution tests.</summary>
    private const string BaseAddress = "http://foo/api/v1/";

    /// <summary>Verifies a leading-slash-less relative path is appended to the base address path under RFC 3986.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Rfc3986AppendsRelativePathToBasePath()
    {
        var captured = await CaptureRfcRequestAsync(BaseAddress, api => api.GetValuesRelative());
        await Assert.That(captured!.AbsoluteUri).IsEqualTo("http://foo/api/v1/values");
    }

    /// <summary>Verifies a dynamic segment is expanded and appended under RFC 3986.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Rfc3986ExpandsDynamicSegment()
    {
        var captured = await CaptureRfcRequestAsync(BaseAddress, api => api.GetUser(7));
        await Assert.That(captured!.AbsoluteUri).IsEqualTo("http://foo/api/v1/users/7");
    }

    /// <summary>Verifies a leading-slash relative path replaces the base address path under RFC 3986.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Rfc3986LeadingSlashReplacesBasePath()
    {
        var captured = await CaptureRfcRequestAsync(BaseAddress, api => api.GetValuesAbsolute());
        await Assert.That(captured!.AbsoluteUri).IsEqualTo("http://foo/values");
    }

    /// <summary>Verifies template and dynamic query parameters are both preserved under RFC 3986.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task Rfc3986PreservesQueryParameters()
    {
        var captured = await CaptureRfcRequestAsync(BaseAddress, api => api.GetValuesWithQuery(3));
        await Assert.That(captured!.AbsoluteUri).IsEqualTo("http://foo/api/v1/values?active=true&page=3");
    }

    /// <summary>Verifies a leading-slash-less route still throws under the default legacy mode.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task LegacyModeRejectsLeadingSlashlessRoute() =>
        await Assert.That(() => RestService.For<IRfcUrlResolutionApi>(BaseAddress)).ThrowsExactly<ArgumentException>();

    /// <summary>Invokes a call under <see cref="UrlResolutionMode.Rfc3986"/> and returns the captured request URI.</summary>
    /// <param name="baseAddress">The client base address.</param>
    /// <param name="call">The interface call to invoke.</param>
    /// <returns>The absolute request URI seen by the handler.</returns>
    private static async Task<Uri?> CaptureRfcRequestAsync(string baseAddress, Func<IRfcUrlResolutionApi, Task<string>> call)
    {
        Uri? captured = null;
        var handler = new StubHttp
        {
            {
                new RouteMatcher
                {
                    Template = "*",
                    Reusable = true
                },
                Reply.From(request =>
                {
                    captured = request.RequestUri;
                    return new(HttpStatusCode.OK)
                    {
                        Content = new StringContent("test")
                    };
                })
            },
        };

        var settings = new RefitSettings
        {
            UrlResolution = UrlResolutionMode.Rfc3986,
            HttpMessageHandlerFactory = () => handler,
        };

        var api = RestService.For<IRfcUrlResolutionApi>(baseAddress, settings);
        _ = await call(api);
        return captured;
    }
}
