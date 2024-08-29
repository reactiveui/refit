using Microsoft.CodeAnalysis.CSharp;

namespace Refit.GeneratorTests.Incremental;

public class FunctionTest
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

    private const string ReturnValueInterface =
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
            Task<int> GetUser(string user);
        }
        """;

    [Fact]
    public void ModifyParameterNameDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // change parameter name
        var newInterface =
            """
            public interface IGitHubApi
            {
                [Get("/users/{myUser}")]
                Task<string> GetUser(string myUser);
            }
            """;
        var compilation2 = TestHelper.ReplaceMemberDeclaration(compilation1, "IGitHubApi", newInterface);

        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    [Fact]
    public void ModifyParameterTypeDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // change parameter type
        var newInterface =
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<string> GetUser(int user);
            }
            """;
        var compilation2 = TestHelper.ReplaceMemberDeclaration(compilation1, "IGitHubApi", newInterface);

        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    [Fact]
    public void ModifyParameterNullabilityDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // change parameter nullability
        var newInterface =
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<string> GetUser(string? user);
            }
            """;
        var compilation2 = TestHelper.ReplaceMemberDeclaration(compilation1, "IGitHubApi", newInterface);

        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    [Fact]
    public void AddParameterDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // add parameter
        var newInterface =
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<string> GetUser(string user, [Query] int myParam);
            }
            """;
        var compilation2 = TestHelper.ReplaceMemberDeclaration(compilation1, "IGitHubApi", newInterface);

        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    [Fact]
    public void ModifyReturnTypeDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // change return type
        var newInterface =
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<int> GetUser(string user);
            }
            """;
        var compilation2 = TestHelper.ReplaceMemberDeclaration(compilation1, "IGitHubApi", newInterface);

        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    [Fact]
    public void ModifyReturnObjectNullabilityDoesNotRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // change return nullability
        var newInterface =
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<string?> GetUser(string user);
            }
            """;
        var compilation2 = TestHelper.ReplaceMemberDeclaration(compilation1, "IGitHubApi", newInterface);

        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Cached);
    }

    [Fact]
    public void ModifyReturnValueNullabilityDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // change return nullability
        var newInterface =
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<int?> GetUser(string user);
            }
            """;
        var compilation2 = TestHelper.ReplaceMemberDeclaration(compilation1, "IGitHubApi", newInterface);

        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    [Fact]
    public void AddNonRefitMethodDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(DefaultInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // change parameter name
        var newInterface =
            """
            public interface IGitHubApi
            {
                [Get("/users/{user}")]
                Task<string> GetUser(string user);

                void NonRefitMethod();
            }
            """;
        var compilation2 = TestHelper.ReplaceMemberDeclaration(compilation1, "IGitHubApi", newInterface);

        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.Modified);
    }
}
