using Microsoft.CodeAnalysis.CSharp;

namespace Refit.GeneratorTests.Incremental;

public class InheritanceTest
{
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

    [Fact]
    public void InheritFromIDisposableDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

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
            """
        );
        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    [Fact]
    public void InheritFromInterfaceDoesRegenerate()
    {
        // TODO: this currently generates invalid code see issue #1801 for more information
        var syntaxTree = CSharpSyntaxTree.ParseText(TwoInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

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
            """
        );
        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached);
    }
}
