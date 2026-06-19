// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.GeneratorTests.Incremental;

/// <summary>Incremental generator tests covering method signature changes.</summary>
public class FunctionTest
{
    /// <summary>Source for an interface with a string-returning method.</summary>
    private const string DefaultInterface =
        """
        #nullable enabled
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

    /// <summary>The name of the interface declaration replaced in these tests.</summary>
    private const string InterfaceName = "IGitHubApi";

    /// <summary>Verifies changing a parameter name triggers regeneration.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ModifyParameterNameDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // change parameter name
        const string newInterface =
            """
            public interface IGitHubApi
            {
                [Get("/users/{myUser}")]
                Task<string> GetUser(string myUser);
            }
            """;
        var compilation2 = TestHelper.ReplaceMemberDeclaration(compilation1, InterfaceName, newInterface);

        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    /// <summary>Verifies changing a parameter type triggers regeneration.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ModifyParameterTypeDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // change parameter type
        const string newInterface =
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<string> GetUser(int user);
            }
            """;
        var compilation2 = TestHelper.ReplaceMemberDeclaration(compilation1, InterfaceName, newInterface);

        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    /// <summary>Verifies changing parameter nullability triggers regeneration.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ModifyParameterNullabilityDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // change parameter nullability
        const string newInterface =
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<string> GetUser(string? user);
            }
            """;
        var compilation2 = TestHelper.ReplaceMemberDeclaration(compilation1, InterfaceName, newInterface);

        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    /// <summary>Verifies adding a parameter triggers regeneration.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AddParameterDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // add parameter
        const string newInterface =
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<string> GetUser(string user, [Query] int myParam);
            }
            """;
        var compilation2 = TestHelper.ReplaceMemberDeclaration(compilation1, InterfaceName, newInterface);

        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    /// <summary>Verifies changing the return type triggers regeneration.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ModifyReturnTypeDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // change return type
        const string newInterface =
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<int> GetUser(string user);
            }
            """;
        var compilation2 = TestHelper.ReplaceMemberDeclaration(compilation1, InterfaceName, newInterface);

        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    /// <summary>Verifies changing reference-type return nullability does not trigger regeneration.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ModifyReturnObjectNullabilityDoesNotRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // change return nullability
        const string newInterface =
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<string?> GetUser(string user);
            }
            """;
        var compilation2 = TestHelper.ReplaceMemberDeclaration(compilation1, InterfaceName, newInterface);

        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached);
    }

    /// <summary>Verifies changing value-type return nullability triggers regeneration.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ModifyReturnValueNullabilityDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // change return nullability
        const string newInterface =
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<int?> GetUser(string user);
            }
            """;
        var compilation2 = TestHelper.ReplaceMemberDeclaration(compilation1, InterfaceName, newInterface);

        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    /// <summary>Verifies adding a non-Refit method triggers regeneration.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task AddNonRefitMethodDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        await TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // change parameter name
        const string newInterface =
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<string> GetUser(string user);

                void NonRefitMethod();
            }
            """;
        var compilation2 = TestHelper.ReplaceMemberDeclaration(compilation1, InterfaceName, newInterface);

        var driver2 = driver1.RunGenerators(compilation2);
        await TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Modified);
    }
}
