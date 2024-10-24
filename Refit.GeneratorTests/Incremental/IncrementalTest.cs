using Microsoft.CodeAnalysis.CSharp;

namespace Refit.GeneratorTests.Incremental;

public class IncrementalTest
{
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

    [Fact]
    public void AddUnrelatedTypeDoesntRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        var compilation2 = compilation1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct MyValue {}"));
        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached);
    }

    [Fact]
    public void SmallChangeDoesntRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // update syntax tree by replacing interface with itself
        var compilation2 = TestHelper.ReplaceMemberDeclaration(
            compilation1,
            "IGitHubApi",
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<string> GetUser(string user);
            }
            """
        );
        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached);
    }

    [Fact]
    public void AddNewMemberDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // add unrelated member, don't change the method
        var compilation2 = TestHelper.ReplaceMemberDeclaration(
            compilation1,
            "IGitHubApi",
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<string> GetUser(string user);

                private record Temp();
            }
            """
        );
        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }
}
