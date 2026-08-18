// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Refit.Tests;

/// <summary>
/// Tests the <see cref="DebuggerDisplayAttribute"/> format strings the shipped types carry.
/// <para>
/// A display made of nothing but one member placeholder renders as a bare <c>null</c> the moment that member is null,
/// which reads in the debugger as though the object itself does not exist. Anyone hovering a response that came back
/// without content sees <c>null</c> and goes looking for a <see cref="NullReferenceException"/> that was never thrown.
/// A display whose only placeholder can be null therefore has to carry literal text alongside it.
/// </para>
/// </summary>
public class DebuggerDisplayTests
{
    /// <summary>The assemblies whose debugger displays a consumer sees.</summary>
    private static readonly Assembly[] ShippedAssemblies =
    [
        typeof(RefitSettings).Assembly,
        typeof(Refit.Testing.StubApiResponse<>).Assembly,
        typeof(SettingsFor<>).Assembly,
        typeof(XmlContentSerializer).Assembly,
    ];

    /// <summary>Verifies no display can render as a bare <c>null</c> with nothing to show an object is there.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Walks the members a debugger display names, which trimming may remove.")]
    public async Task NoDisplayCanRenderAsABareNull()
    {
        var offenders = new List<string>();

        foreach (var (type, display) in DisplayedTypes())
        {
            if (SoleMemberPlaceholder(display) is not { } member)
            {
                continue;
            }

            if (ResolveMember(type, member) is { } resolved && CanBeNull(resolved))
            {
                offenders.Add($"{type.FullName} -> \"{display}\"");
            }
        }

        await Assert.That(offenders.Order(StringComparer.Ordinal).ToArray()).IsEmpty();
    }

    /// <summary>Verifies every placeholder names a member that exists, so no display silently shows an evaluation error.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Walks the members a debugger display names, which trimming may remove.")]
    public async Task EveryMemberPlaceholderResolves()
    {
        var unresolved = new List<string>();

        foreach (var (type, display) in DisplayedTypes())
        {
            foreach (var placeholder in Placeholders(display))
            {
                // Method-call placeholders such as ToString() are evaluated by the debugger, not looked up here.
                if (placeholder.Contains('(', StringComparison.Ordinal) || ResolveMember(type, placeholder) is not null)
                {
                    continue;
                }

                unresolved.Add($"{type.FullName} -> \"{display}\" ({placeholder})");
            }
        }

        await Assert.That(unresolved.Order(StringComparer.Ordinal).ToArray()).IsEmpty();
    }

    /// <summary>Enumerates every shipped type carrying a debugger display, with its format string.</summary>
    /// <returns>The type and display pairs.</returns>
    [RequiresUnreferencedCode("Enumerates shipped types, which trimming may remove.")]
    private static IEnumerable<(Type Type, string Display)> DisplayedTypes()
    {
        foreach (var assembly in ShippedAssemblies)
        {
            foreach (var type in assembly.GetExportedTypes())
            {
                if (type.GetCustomAttribute<DebuggerDisplayAttribute>(inherit: false)?.Value is { } display)
                {
                    yield return (type, display);
                }
            }
        }
    }

    /// <summary>Reads the expressions a display interpolates, stripping any format specifier.</summary>
    /// <param name="display">The format string.</param>
    /// <returns>The placeholder expressions, in order.</returns>
    private static List<string> Placeholders(string display)
    {
        var found = new List<string>();
        var open = display.IndexOf('{', StringComparison.Ordinal);

        while (open >= 0)
        {
            var close = display.IndexOf('}', open);
            if (close < 0)
            {
                break;
            }

            var expression = display[(open + 1)..close];
            var comma = expression.IndexOf(',', StringComparison.Ordinal);
            found.Add(comma < 0 ? expression : expression[..comma]);
            open = display.IndexOf('{', close);
        }

        return found;
    }

    /// <summary>Reads the member a display names when the display is that placeholder and nothing else.</summary>
    /// <param name="display">The format string.</param>
    /// <returns>The member name, or <see langword="null"/> when the display carries literal text or calls a method.</returns>
    private static string? SoleMemberPlaceholder(string display)
    {
        var placeholders = Placeholders(display);
        if (placeholders.Count != 1 || placeholders[0].Contains('(', StringComparison.Ordinal))
        {
            return null;
        }

        // Literal text either side of the placeholder is what keeps a null value legible, so a display that has any
        // is already safe.
        return display.StartsWith('{') && display.EndsWith('}') ? placeholders[0] : null;
    }

    /// <summary>Resolves a placeholder to the property or field it names.</summary>
    /// <param name="type">The type carrying the display.</param>
    /// <param name="name">The member name.</param>
    /// <returns>The member, or <see langword="null"/> when the type exposes none by that name.</returns>
    [RequiresUnreferencedCode("Looks up a member by name, which trimming may remove.")]
    private static MemberInfo? ResolveMember(Type type, string name)
    {
        const BindingFlags Flags =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        // Walked most-derived first, because a derived member shadowing a base one is what the debugger evaluates and
        // a flattened lookup cannot tell the two apart.
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (((MemberInfo?)current.GetProperty(name, Flags) ?? current.GetField(name, Flags)) is { } member)
            {
                return member;
            }
        }

        return null;
    }

    /// <summary>Determines whether a member's value can be null.</summary>
    /// <param name="member">The property or field a display names.</param>
    /// <returns><see langword="true"/> when the member is a nullable value type or a nullable-annotated reference type.</returns>
    private static bool CanBeNull(MemberInfo member)
    {
        var context = new NullabilityInfoContext();
        var info = member is PropertyInfo property ? context.Create(property) : context.Create((FieldInfo)member);

        return Nullable.GetUnderlyingType(info.Type) is not null
            || info.ReadState == NullabilityState.Nullable
            || (!info.Type.IsValueType && info.ReadState == NullabilityState.Unknown);
    }
}
