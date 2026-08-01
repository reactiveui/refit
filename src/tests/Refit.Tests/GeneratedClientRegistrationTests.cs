// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System;
using System.Net.Http;

namespace Refit.Tests;

/// <summary>
/// Verifies client resolution copes with a module initializer that has not run yet. Generated factories register from
/// a <c>[ModuleInitializer]</c> in the interface's own assembly, and the runtime only promises to run that before the
/// first static field read or method call in the module. Resolving a client only does <c>typeof(T)</c>, so Mono -
/// which Blazor WebAssembly runs on - can still have the registration pending when the first lookup happens.
/// </summary>
public sealed class GeneratedClientRegistrationTests
{
    /// <summary>The base address given to every client under test.</summary>
    private static readonly Uri _baseAddress = new("http://api/");

    /// <summary>Verifies a generated client registered while the initializer runs is picked up by the retried lookup.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedClientResolvesWhenRegistrationArrivesWithTheModuleInitializer()
    {
        var stub = new ClientRegistrationStub();
        using var client = HttpClientTestFactory.Create(_baseAddress);

        var resolved = RestService.TryResolveGeneratedClient<IGeneratedClientRegistrationApi>(
            client,
            new(),
            _ => RestService.RegisterGeneratedFactory<IGeneratedClientRegistrationApi>((_, _) => stub),
            out var instance);

        await Assert.That(resolved).IsTrue();
        await Assert.That(instance).IsSameReferenceAs(stub);
    }

    /// <summary>Verifies the type-keyed generated client is picked up by the retried lookup.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GeneratedClientResolvesByTypeWhenRegistrationArrivesWithTheModuleInitializer()
    {
        var stub = new ClientRegistrationStub();
        using var client = HttpClientTestFactory.Create(_baseAddress);

        var resolved = RestService.TryResolveGeneratedClient(
            typeof(IGeneratedClientRegistrationByTypeApi),
            client,
            new(),
            _ => RestService.RegisterGeneratedFactory(
                typeof(IGeneratedClientRegistrationByTypeApi),
                (_, _) => stub),
            out var instance);

        await Assert.That(resolved).IsTrue();
        await Assert.That(instance).IsSameReferenceAs(stub);
    }

    /// <summary>Verifies an inline client registered while the initializer runs is used instead of the reflection request builder.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InlineClientResolvesWhenRegistrationArrivesWithTheModuleInitializer()
    {
        var stub = new ClientRegistrationStub();
        using var client = HttpClientTestFactory.Create(_baseAddress);

        var resolved = RestService.TryResolveInlineClient<IInlineClientRegistrationApi>(
            client,
            new(),
            _ => RestService.RegisterGeneratedSettingsFactory<IInlineClientRegistrationApi>((_, _) => stub),
            out var instance);

        await Assert.That(resolved).IsTrue();
        await Assert.That(instance).IsSameReferenceAs(stub);
    }

    /// <summary>Verifies the type-keyed inline client is picked up by the retried lookup.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InlineClientResolvesByTypeWhenRegistrationArrivesWithTheModuleInitializer()
    {
        var stub = new ClientRegistrationStub();
        using var client = HttpClientTestFactory.Create(_baseAddress);

        var resolved = RestService.TryResolveInlineClient(
            typeof(IInlineClientRegistrationByTypeApi),
            client,
            new(),
            _ => RestService.RegisterGeneratedSettingsFactory<IInlineClientRegistrationByTypeApi>((_, _) => stub),
            out var instance);

        await Assert.That(resolved).IsTrue();
        await Assert.That(instance).IsSameReferenceAs(stub);
    }

    /// <summary>Verifies resolution reports failure when the initializer registers nothing for the interface.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ClientResolutionFailsWhenNoRegistrationArrives()
    {
        using var client = HttpClientTestFactory.Create(_baseAddress);
        var settings = new RefitSettings();
        var interfaceType = typeof(IAmNotARefitInterface);

        var resolvedGenerated = RestService.TryResolveGeneratedClient<IAmNotARefitInterface>(
            client,
            settings,
            static _ => { },
            out _);

        var resolvedByType = RestService.TryResolveGeneratedClient(
            interfaceType,
            client,
            settings,
            static _ => { },
            out _);

        var resolvedInline = RestService.TryResolveInlineClient<IAmNotARefitInterface>(
            client,
            settings,
            static _ => { },
            out _);

        var resolvedInlineByType = RestService.TryResolveInlineClient(
            interfaceType,
            client,
            settings,
            static _ => { },
            out _);

        await Assert.That(resolvedGenerated).IsFalse();
        await Assert.That(resolvedByType).IsFalse();
        await Assert.That(resolvedInline).IsFalse();
        await Assert.That(resolvedInlineByType).IsFalse();
    }

    /// <summary>Verifies forcing the interface assembly's module constructor is safe to repeat and keeps the client resolvable.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RunningGeneratedRegistrationsRepeatedlyKeepsTheClientResolvable()
    {
        RestService.RunGeneratedRegistrations(typeof(IRoundTripNotString));
        RestService.RunGeneratedRegistrations(typeof(IRoundTripNotString));

        using var client = HttpClientTestFactory.Create(_baseAddress);

        await Assert.That(RestService.ForGenerated<IRoundTripNotString>(client)).IsNotNull();
    }

    /// <summary>Verifies an interface with no generated client still reports that, after the initializer is forced.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ForGeneratedReportsAMissingGeneratedClient()
    {
        using var client = HttpClientTestFactory.Create(_baseAddress);

        await Assert.That(() => RestService.ForGenerated<IAmNotARefitInterface>(client))
            .ThrowsExactly<InvalidOperationException>()
            .WithMessage(
                "IAmNotARefitInterface doesn't look like a Refit interface. Make sure it has at least one method with "
                + "a Refit HTTP method attribute, the Refit source generator is installed in the project, and your "
                + "build produced the generated client. For Native AOT or trimmed apps, prefer generated clients plus "
                + "source-generated System.Text.Json metadata.",
                StringComparison.Ordinal);
    }

    /// <summary>Verifies resolving by type also reports a missing generated client.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ForGeneratedByTypeReportsAMissingGeneratedClient()
    {
        using var client = HttpClientTestFactory.Create(_baseAddress);
        var interfaceType = typeof(IAmNotARefitInterface);

        var settings = new RefitSettings();

        await Assert.That(() => RestService.ForGenerated(interfaceType, client, settings))
            .ThrowsExactly<InvalidOperationException>();
    }
}
