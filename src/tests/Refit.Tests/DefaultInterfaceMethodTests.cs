// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Threading.Tasks;
using Refit.Testing;

namespace Refit.Tests;

// DIMs require C# 8.0 which requires .NET Core 3.x or .NET Standard 2.1
#if NETCOREAPP3_1_OR_GREATER

/// <summary>Tests covering Refit support for default, internal and static interface members.</summary>
public class DefaultInterfaceMethodTests
{
    /// <summary>The base address and stub URL used across the tests.</summary>
    private const string HttpBinUrl = "https://httpbin.org/";

    /// <summary>Verifies an internal interface member can be invoked through Refit.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task InternalInterfaceMemberTest()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(HttpBinUrl),
                new StubResponse { Status = HttpStatusCode.OK, Text = "OK", ContentType = "text/html" }
            },
        };

        var settings = handler.ToSettings();

        var fixture = RestService.For<IHaveDims>(HttpBinUrl, settings);
        var plainText = await fixture.GetInternal();

        await Assert.That(!string.IsNullOrWhiteSpace(plainText)).IsTrue();
    }

    /// <summary>Verifies a default interface method can be invoked through Refit.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DimTest()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(HttpBinUrl),
                new StubResponse { Status = HttpStatusCode.OK, Text = "OK", ContentType = "text/html" }
            },
        };

        var settings = handler.ToSettings();

        var fixture = RestService.For<IHaveDims>(HttpBinUrl, settings);
        var plainText = await fixture.GetDim();

        await Assert.That(!string.IsNullOrWhiteSpace(plainText)).IsTrue();
    }

    /// <summary>Verifies an internal interface member invoked through Refit returns the expected body.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task InternalDimTest()
    {
        var handler = new StubHttp
        {
            {
                Route.Get(HttpBinUrl),
                new StubResponse { Status = HttpStatusCode.OK, Text = "OK", ContentType = "text/html" }
            },
        };

        var settings = handler.ToSettings();

        var fixture = RestService.For<IHaveDims>(HttpBinUrl, settings);
        var plainText = await fixture.GetInternal();

        await Assert.That(plainText).IsEqualTo("OK");
    }

    /// <summary>Verifies a static interface method can be invoked directly.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task StaticInterfaceMethodTest()
    {
        var plainText = IHaveDims.GetStatic();

        await Assert.That(!string.IsNullOrWhiteSpace(plainText)).IsTrue();
    }
}
#endif
