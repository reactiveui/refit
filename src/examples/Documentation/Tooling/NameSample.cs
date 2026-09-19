// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
extern alias GeneratorTooling;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UniqueNameBuilder = GeneratorTooling::Refit.Generator.UniqueNameBuilder;
using WellKnownTypes = GeneratorTooling::Refit.Generator.WellKnownTypes;

namespace Refit.Documentation.Tooling;

/// <summary>Checks identifier reservation and compiler type lookup.</summary>
internal static class NameSample
{
    /// <summary>Reserves a suffixed name to show how the next free suffix is chosen.</summary>
    private static readonly string[] ReservedNames = ["client0", "response"];

    /// <summary>Demonstrates both reservation overloads and both public type lookup methods.</summary>
    internal static void Run()
    {
        const string identifier = "client";
        UniqueNameBuilder names = new();
        names.Reserve(identifier);
        names.Reserve(ReservedNames);
        string next = names.New(identifier);
        Console.WriteLine(next); // client1

        Check.Require(next == "client1", "Reserved identifiers must be skipped.");
        Check.Require(names.New(identifier) == "client2", "Returned identifiers must be reserved.");
        Check.Require(names.New("response") == "response0", "Enumerable reservations also block their original name.");
        Check.Require(names.New("Client") == "Client", "Identifier reservation uses case-sensitive comparisons.");
        CSharpCompilation compilation = ToolingCompilation.Create("internal class Model;");

        WellKnownTypes types = new(compilation);
        INamedTypeSymbol stringType = types.Get(typeof(string));
        INamedTypeSymbol? missing = types.TryGet("Demo.Missing");
        Console.WriteLine(stringType.Name); // String
        Console.WriteLine(missing is null); // True

        Check.Require(stringType.SpecialType == SpecialType.System_String && missing is null, "Type lookup must use compilation metadata names.");
        Check.Require(ReferenceEquals(types.Get(typeof(string)), stringType) && ReferenceEquals(types.TryGet("System.String"), stringType), "Both lookup methods share the cached compiler symbol.");
        Check.Require(types.TryGet("Demo.Missing") is null, "Repeated absent lookups retain the null result.");
        bool rejectedMissing = false;
        try
        {
            _ = types.Get(typeof(NameSample));
        }
        catch (InvalidOperationException exception)
        {
            rejectedMissing = exception.Message == $"Could not get type {typeof(NameSample).FullName}";
        }

        Check.Require(rejectedMissing, "Required lookup rejects types absent from the compiler references.");
        CheckUnnamedType(types);
    }

    /// <summary>Checks required lookup for a generic parameter that has no metadata full name.</summary>
    /// <param name="types">The compilation-bound lookup helper.</param>
    private static void CheckUnnamedType(WellKnownTypes types)
    {
        Type genericParameter = typeof(List<>).GetGenericArguments()[0];
        bool rejected = false;
        try
        {
            _ = types.Get(genericParameter);
        }
        catch (InvalidOperationException exception)
        {
            rejected = exception.Message == $"Could not get name of type {genericParameter.Name}";
        }

        Check.Require(rejected, "Required lookup rejects runtime types with no metadata full name.");
    }
}
