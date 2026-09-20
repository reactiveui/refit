// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Verifies which absolute links <see cref="NextLinkOriginPolicy"/> allows a paging helper to follow.</summary>
public class NextLinkOriginPolicyTests
{
    /// <summary>The origin the tests treat as the API's own.</summary>
    private const string ApiOrigin = "https://api.example.com";

    /// <summary>A second origin used to exercise allow-lists.</summary>
    private const string CdnOrigin = "https://cdn.example.com";

    /// <summary>The API's own origin as a URI.</summary>
    private static readonly Uri ApiOriginUri = new(ApiOrigin);

    /// <summary>The second origin as a URI.</summary>
    private static readonly Uri CdnOriginUri = new(CdnOrigin);

    /// <summary>Verifies a link on an allowed origin is permitted whatever its path and query.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Allow_LinkOnAllowedOrigin_IsAllowed()
    {
        var policy = NextLinkOriginPolicy.Allow(new Uri($"{ApiOrigin}/v1/"));

        await Assert.That(policy.IsAllowed(new($"{ApiOrigin}/v2/items?page=2#top"))).IsTrue();
    }

    /// <summary>Verifies a link on a different host is refused.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Allow_LinkOnOtherHost_IsRefused()
    {
        var policy = NextLinkOriginPolicy.Allow(ApiOriginUri);

        await Assert.That(policy.IsAllowed(new("https://evil.example.net/items?page=2"))).IsFalse();
    }

    /// <summary>Verifies a host that merely begins with the allowed host is refused.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Allow_LinkOnLookalikeHost_IsRefused()
    {
        var policy = NextLinkOriginPolicy.Allow(ApiOriginUri);

        await Assert.That(policy.IsAllowed(new("https://api.example.com.evil.net/items"))).IsFalse();
    }

    /// <summary>Verifies a scheme downgrade on the allowed host is refused.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Allow_SchemeDowngrade_IsRefused()
    {
        var policy = NextLinkOriginPolicy.Allow(ApiOriginUri);

        await Assert.That(policy.IsAllowed(new("http://api.example.com/items"))).IsFalse();
    }

    /// <summary>Verifies a different port on the allowed host is refused.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Allow_DifferentPort_IsRefused()
    {
        var policy = NextLinkOriginPolicy.Allow(ApiOriginUri);

        await Assert.That(policy.IsAllowed(new("https://api.example.com:8443/items"))).IsFalse();
    }

    /// <summary>Verifies an explicit default port and a differently cased host match an origin that omits the port.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Allow_ExplicitDefaultPortAndHostCase_MatchTheOrigin()
    {
        var policy = NextLinkOriginPolicy.Allow(ApiOriginUri);

        await Assert.That(policy.IsAllowed(new("https://API.example.com:443/items"))).IsTrue();
    }

    /// <summary>Verifies user information in a link is refused even on an allowed origin.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Allow_LinkWithUserInfo_IsRefused()
    {
        var policy = NextLinkOriginPolicy.Allow(ApiOriginUri);

        await Assert.That(policy.IsAllowed(new("https://user:secret@api.example.com/items"))).IsFalse();
    }

    /// <summary>Verifies a relative link is refused because it names no origin.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Allow_RelativeLink_IsRefused()
    {
        var policy = NextLinkOriginPolicy.Allow(ApiOriginUri);

        await Assert.That(policy.IsAllowed(new("/items?page=2", UriKind.Relative))).IsFalse();
    }

    /// <summary>Verifies every origin of a multi-origin allow-list is permitted and others are not.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Allow_MultipleOrigins_PermitsEachOfThem()
    {
        var policy = NextLinkOriginPolicy.Allow(ApiOriginUri, CdnOriginUri);

        using (Assert.Multiple())
        {
            await Assert.That(policy.IsAllowed(new($"{ApiOrigin}/a"))).IsTrue();
            await Assert.That(policy.IsAllowed(new($"{CdnOrigin}/b"))).IsTrue();
            await Assert.That(policy.IsAllowed(new("https://other.example.com/c"))).IsFalse();
        }
    }

    /// <summary>Verifies the unrestricted policy permits any absolute http or https link.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Unrestricted_AnyAbsoluteHttpLink_IsAllowed()
    {
        var policy = NextLinkOriginPolicy.Unrestricted;

        using (Assert.Multiple())
        {
            await Assert.That(policy.IsAllowed(new("https://anywhere.example.org/x"))).IsTrue();
            await Assert.That(policy.IsAllowed(new("http://anywhere.example.org/x"))).IsTrue();
        }
    }

    /// <summary>Verifies the unrestricted policy still refuses non-http schemes, relative links and user information.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Unrestricted_NonHttpRelativeOrUserInfoLink_IsRefused()
    {
        var policy = NextLinkOriginPolicy.Unrestricted;

        using (Assert.Multiple())
        {
            await Assert.That(policy.IsAllowed(new("ftp://files.example.org/x"))).IsFalse();
            await Assert.That(policy.IsAllowed(new("file:///etc/passwd"))).IsFalse();
            await Assert.That(policy.IsAllowed(new("/x", UriKind.Relative))).IsFalse();
            await Assert.That(policy.IsAllowed(new("https://user:pw@anywhere.example.org/x"))).IsFalse();
        }
    }

    /// <summary>Verifies a relative link resolves against the referrer when one is known.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Resolve_RelativeLink_UsesTheReferrer()
    {
        var policy = NextLinkOriginPolicy.Allow(ApiOriginUri);

        var resolved = policy.Resolve(new("items?page=2", UriKind.Relative), new($"{ApiOrigin}/v1/items?page=1"));

        await Assert.That(resolved.AbsoluteUri).IsEqualTo($"{ApiOrigin}/v1/items?page=2");
    }

    /// <summary>Verifies a relative link resolves against the first allowed origin when no referrer is known.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Resolve_RelativeLinkWithoutReferrer_UsesTheFirstAllowedOrigin()
    {
        var policy = NextLinkOriginPolicy.Allow(ApiOriginUri, CdnOriginUri);

        var resolved = policy.Resolve(new("/items?page=2", UriKind.Relative), null);

        await Assert.That(resolved.AbsoluteUri).IsEqualTo($"{ApiOrigin}/items?page=2");
    }

    /// <summary>Verifies a relative link cannot be resolved by an unrestricted policy that has no referrer.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Resolve_RelativeLinkWithoutAnyBase_Throws() =>
        await Assert
            .That(static () => NextLinkOriginPolicy.Unrestricted.Resolve(new("/items", UriKind.Relative), null))
            .Throws<InvalidOperationException>();

    /// <summary>Verifies a refused link is reported by origin only, so its query string never reaches the message.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Resolve_RefusedLink_NamesTheOriginButNotTheQuery()
    {
        var policy = NextLinkOriginPolicy.Allow(ApiOriginUri);

        var failure = await Assert
            .That(() => policy.Resolve(new("https://evil.example.net/items?secret=abc"), null))
            .Throws<InvalidOperationException>();

        using (Assert.Multiple())
        {
            await Assert.That(failure!.Message.Contains("https://evil.example.net", StringComparison.Ordinal)).IsTrue();
            await Assert.That(failure.Message.Contains("secret=abc", StringComparison.Ordinal)).IsFalse();
        }
    }

    /// <summary>Verifies an allowed absolute link is returned unchanged.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Resolve_AllowedAbsoluteLink_IsReturnedUnchanged()
    {
        Uri link = new($"{ApiOrigin}/items?page=2");

        var resolved = NextLinkOriginPolicy.Allow(ApiOriginUri).Resolve(link, null);

        await Assert.That(resolved).IsSameReferenceAs(link);
    }

    /// <summary>Verifies the policies describe themselves for the debugger.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ToString_DescribesTheAllowedOrigins()
    {
        var allowed = NextLinkOriginPolicy.Allow(new Uri($"{ApiOrigin}/v1"), CdnOriginUri);

        using (Assert.Multiple())
        {
            await Assert.That(NextLinkOriginPolicy.Unrestricted.ToString()).IsEqualTo("Unrestricted");
            await Assert.That(allowed.ToString()).IsEqualTo($"Allow({ApiOrigin}, {CdnOrigin})");
        }
    }

    /// <summary>Verifies a null link is rejected.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task IsAllowed_NullLink_Throws() =>
        await Assert.That(static () => NextLinkOriginPolicy.Unrestricted.IsAllowed(null!)).Throws<ArgumentNullException>();

    /// <summary>Verifies an allow-list without origins is rejected.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Allow_NoOrigins_Throws() =>
        await Assert.That(static () => NextLinkOriginPolicy.Allow()).Throws<ArgumentException>();

    /// <summary>Verifies an allow-list containing a null origin is rejected.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Allow_NullOrigin_Throws() =>
        await Assert.That(static () => NextLinkOriginPolicy.Allow(ApiOriginUri, null!)).Throws<ArgumentNullException>();

    /// <summary>Verifies a null origin array is rejected.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task Allow_NullArray_Throws() =>
        await Assert.That(static () => NextLinkOriginPolicy.Allow(null!)).Throws<ArgumentNullException>();

    /// <summary>Verifies an origin that is relative or not http or https is rejected.</summary>
    /// <param name="origin">The invalid origin under test.</param>
    /// <param name="kind">Whether <paramref name="origin"/> is absolute or relative.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments("/relative", UriKind.Relative)]
    [Arguments("ftp://files.example.org", UriKind.Absolute)]
    public async Task Allow_InvalidOrigin_Throws(string origin, UriKind kind) =>
        await Assert.That(() => NextLinkOriginPolicy.Allow(new Uri(origin, kind))).Throws<ArgumentException>();
}
