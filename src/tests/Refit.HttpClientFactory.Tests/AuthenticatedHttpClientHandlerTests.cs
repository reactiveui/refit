// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
extern alias RefitHttpClientFactory;

using AuthenticatedHttpClientHandler = RefitHttpClientFactory::Refit.AuthenticatedHttpClientHandler;

namespace Refit.HttpClientFactory.Tests;

/// <summary>Direct contract tests for the authenticated handler compiled into Refit.HttpClientFactory.</summary>
public sealed class AuthenticatedHttpClientHandlerTests
{
    /// <summary>Verifies both constructors handle default, null, and explicit inner handlers.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ConstructorsRetainTheirDocumentedInnerHandlerBehavior()
    {
        using var legacyDefault = new AuthenticatedHttpClientHandler(
            static (_, _) => new ValueTask<string>(string.Empty));
        using var legacyInner = new AuthenticatedHttpClientHandler(
            static (_, _) => new ValueTask<string>(string.Empty),
            new TerminalHandler());
        using var modernNull = new AuthenticatedHttpClientHandler(
            null,
            static (_, _) => new ValueTask<string>(string.Empty));
        using var modernInner = new AuthenticatedHttpClientHandler(
            new TerminalHandler(),
            static (_, _) => new ValueTask<string>(string.Empty));

        await Assert.That(legacyDefault.InnerHandler).IsTypeOf<HttpClientHandler>();
        await Assert.That(legacyInner.InnerHandler).IsTypeOf<TerminalHandler>();
        await Assert.That(modernNull.InnerHandler).IsNull();
        await Assert.That(modernInner.InnerHandler).IsTypeOf<TerminalHandler>();
    }

    /// <summary>Verifies blank tokens are removed and populated tokens replace the placeholder value.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AuthorizationTokensAreRemovedOrReplaced()
    {
        var blankTerminal = new TerminalHandler();
        using var blankHandler = new AuthenticatedHttpClientHandler(
            blankTerminal,
            static (_, _) => new ValueTask<string>(" "));
        using var blankInvoker = new HttpMessageInvoker(blankHandler, disposeHandler: false);
        using var blankRequest = CreateAuthorizedRequest();
        using var blankResponse = await blankInvoker.SendAsync(blankRequest, CancellationToken.None);

        var tokenTerminal = new TerminalHandler();
        using var tokenHandler = new AuthenticatedHttpClientHandler(
            tokenTerminal,
            static (_, _) => new ValueTask<string>("token"));
        using var tokenInvoker = new HttpMessageInvoker(tokenHandler, disposeHandler: false);
        using var tokenRequest = CreateAuthorizedRequest();
        using var tokenResponse = await tokenInvoker.SendAsync(tokenRequest, CancellationToken.None);

        await Assert.That(blankTerminal.Authorization).IsNull();
        await Assert.That(tokenTerminal.Authorization?.Parameter).IsEqualTo("token");
    }

    /// <summary>Creates a request carrying the placeholder authorization value replaced by the handler.</summary>
    /// <returns>The request.</returns>
    private static HttpRequestMessage CreateAuthorizedRequest() =>
        new(HttpMethod.Get, "https://example.test")
        {
            Headers = { Authorization = new("Bearer", "placeholder") }
        };
}
