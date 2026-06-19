// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.GeneratorTests.Incremental;

/// <summary>Tests that interface inheritance changes trigger incremental regeneration.</summary>
public class InheritanceTest
{
    /// <summary>The source for a single interface used as the baseline.</summary>
    private const string DefaultInterface =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Net.Http;
        using System.Text;
        using System.Threading;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public interface IGitHubApi
        {
            [Get("/users/{user}")]
            Task<string> GetUser(string user);
        }
        """;

    /// <summary>The source containing two interfaces used for inheritance scenarios.</summary>
    private const string TwoInterface =
        """
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Net.Http;
        using System.Text;
        using System.Threading;
        using System.Threading.Tasks;
        using Refit;

        namespace RefitGeneratorTest;

        public interface IGitHubApi
        {
            [Get("/users/{user}")]
            Task<string> GetUser(string user);
        }

        public interface IBaseInterface { void NonRefitMethod(); }
        """;

    /// <summary>Verifies that adding inheritance from IDisposable regenerates the output.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task InheritFromIDisposableDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // inherit from IDisposable
        var compilation2 = TestHelper.ReplaceMemberDeclaration(
            compilation1,
            "IGitHubApi",
            """
            public interface IGitHubApi : IDisposable
            {
                [Get("/users/{user}")]
                Task<string> GetUser(string user);
            }
            """);
        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    /// <summary>Verifies that adding inheritance from another interface regenerates the output.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task InheritFromInterfaceDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(TwoInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // inherit from second interface
        var compilation2 = TestHelper.ReplaceMemberDeclaration(
            compilation1,
            "IGitHubApi",
            """
            public interface IGitHubApi : IBaseInterface
            {
                [Get("/users/{user}")]
                Task<string> GetUser(string user);
            }
            """);
        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Modified);
    }
}
