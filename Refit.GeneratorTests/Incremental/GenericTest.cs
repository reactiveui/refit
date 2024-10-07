using Microsoft.CodeAnalysis.CSharp;

namespace Refit.GeneratorTests.Incremental;

public class GenericTest
{
    private const string GenericInterface =
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

        public interface IGeneratedInterface<T1>
        {
            [Get("/users")]
            Task<string> Get();
        }
        """;

    // [Fact]
    public void RenameGenericTypeDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(GenericInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // rename generic type
        var compilation2 = TestHelper.ReplaceMemberDeclaration(
            compilation1,
            "IGeneratedInterface",
            """
            public interface IGeneratedInterface<T>
            {
                [Get("/users")]
                Task<string> Get();
            }
            """
        );
        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    // [Fact]
    public void AddGenericConstraintDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(GenericInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // add generic constraint
        var compilation2 = TestHelper.ReplaceMemberDeclaration(
            compilation1,
            "IGeneratedInterface",
            """
            public interface IGeneratedInterface<T1>
                where T1 : class
            {
                [Get("/users")]
                Task<string> Get();
            }
            """
        );
        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);

        // add new generic constraint
        var compilation3 = TestHelper.ReplaceMemberDeclaration(
            compilation2,
            "IGeneratedInterface",
            """
            public interface IGeneratedInterface<T1>
                where T1 : class, new()
            {
                [Get("/users")]
                Task<string> Get();
            }
            """
        );
        var driver3 = driver2.RunGenerators(compilation3);
        TestHelper.AssertRunReasons(driver3, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    // [Fact]
    public void AddObjectGenericConstraintDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(GenericInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // add object generic constraint
        var compilation2 = TestHelper.ReplaceMemberDeclaration(
            compilation1,
            "IGeneratedInterface",
            """
            public interface IGeneratedInterface<T1>
                where T1 : IDisposable
            {
                [Get("/users")]
                Task<string> Get();
            }
            """
        );
        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }

    // [Fact]
    public void AddGenericTypeDoesRegenerate()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(GenericInterface, CSharpParseOptions.Default);
        var compilation1 = Fixture.CreateLibrary(syntaxTree);

        var driver1 = TestHelper.GenerateTracked(compilation1);
        TestHelper.AssertRunReasons(driver1, IncrementalGeneratorRunReasons.New);

        // add second generic type
        var compilation2 = TestHelper.ReplaceMemberDeclaration(
            compilation1,
            "IGeneratedInterface",
            """
            public interface IGeneratedInterface<T1, T2>
            {
                [Get("/users")]
                Task<string> Get();
            }
            """
        );
        var driver2 = driver1.RunGenerators(compilation2);
        TestHelper.AssertRunReasons(driver2, IncrementalGeneratorRunReasons.ModifiedSource);
    }
}
