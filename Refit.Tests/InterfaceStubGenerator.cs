using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using Nustache;
using Nustache.Core;
using Refit; // InterfaceStubGenerator looks for this
using Refit.Generator;
using Task = System.Threading.Tasks.Task;

namespace Refit.Tests
{
    public class InterfaceStubGeneratorTests
    {
        [Fact]
        public void GenerateInterfaceStubsSmokeTest()
        {
            var fixture = new InterfaceStubGenerator();

            var result = fixture.GenerateInterfaceStubs(new[] {
                IntegrationTestHelper.GetPath("RestService.cs"),
                IntegrationTestHelper.GetPath("GitHubApi.cs"),
            });

            Assert.True(result.Contains("IGitHubApi"));
        }

        [Fact]
        public void FindInterfacesSmokeTest()
        {
            var input = IntegrationTestHelper.GetPath("GitHubApi.cs");
            var fixture = new InterfaceStubGenerator();

            var result = fixture.FindInterfacesToGenerate(CSharpSyntaxTree.ParseText(File.ReadAllText(input)));
            Assert.Equal(1, result.Count);
            Assert.True(result.Any(x => x.Identifier.ValueText == "IGitHubApi"));

            input = IntegrationTestHelper.GetPath("InterfaceStubGenerator.cs");

            result = fixture.FindInterfacesToGenerate(CSharpSyntaxTree.ParseText(File.ReadAllText(input)));
            Assert.Equal(2, result.Count);
            Assert.True(result.Any(x => x.Identifier.ValueText == "IAmARefitInterfaceButNobodyUsesMe"));
            Assert.True(result.Any(x => x.Identifier.ValueText == "IBoringCrudApi"));
            Assert.True(result.All(x => x.Identifier.ValueText != "IAmNotARefitInterface"));
        }

        [Fact]
        public void HasRefitHttpMethodAttributeSmokeTest()
        {
            var file = CSharpSyntaxTree.ParseText(File.ReadAllText(IntegrationTestHelper.GetPath("InterfaceStubGenerator.cs")));
            var fixture = new InterfaceStubGenerator();

            var input = file.GetRoot().DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>()
                .SelectMany(i => i.Members.OfType<MethodDeclarationSyntax>())
                .ToList();

            var result = input
                .ToDictionary(m => m.Identifier.ValueText, fixture.HasRefitHttpMethodAttribute);

            Assert.True(result["RefitMethod"]);
            Assert.True(result["AnotherRefitMethod"]);
            Assert.False(result["NoConstantsAllowed"]);
            Assert.False(result["NotARefitMethod"]);
            Assert.True(result["ReadOne"]);
            Assert.True(result["SpacesShouldntBreakMe"]);
        }

        [Fact]
        public void GenerateClassInfoForInterfaceSmokeTest()
        {
            var file = CSharpSyntaxTree.ParseText(File.ReadAllText(IntegrationTestHelper.GetPath("GitHubApi.cs")));
            var fixture = new InterfaceStubGenerator();

            var input = file.GetRoot().DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>()
                .First(x => x.Identifier.ValueText == "IGitHubApi");

            var result = fixture.GenerateClassInfoForInterface(input);

            Assert.Equal(8, result.MethodList.Count);
            Assert.Equal("GetUser", result.MethodList[0].Name);
            Assert.Equal("string userName", result.MethodList[0].ArgumentListWithTypes);
        }

        [Fact]
        public void GenerateTemplateInfoForInterfaceListSmokeTest()
        {
            var file = CSharpSyntaxTree.ParseText(File.ReadAllText(IntegrationTestHelper.GetPath("RestService.cs")));
            var fixture = new InterfaceStubGenerator();

            var input = file.GetRoot().DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>()
                .ToList();

            var result = fixture.GenerateTemplateInfoForInterfaceList(input);
            Assert.Equal(8, result.ClassList.Count);
        }

        [Fact]
        public void RetainsAliasesInUsings()
        {
            var fixture = new InterfaceStubGenerator();

            var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(IntegrationTestHelper.GetPath("NamespaceCollisionApi.cs")));
            var interfaceDefinition = syntaxTree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            var result = fixture.GenerateTemplateInfoForInterfaceList(new List<InterfaceDeclarationSyntax>(interfaceDefinition));

            var usingList = result.UsingList.Select(x => x.Item).ToList();
            Assert.Contains("SomeType =  CollisionA.SomeType", usingList);
            Assert.Contains("CollisionB", usingList);
        }
    }

    public static class ThisIsDumbButMightHappen
    {
        public const string PeopleDoWeirdStuff = "But we don't let them";
    }

    public interface IAmARefitInterfaceButNobodyUsesMe
    {
        [Get("whatever")]
        Task RefitMethod();

        [Refit.GetAttribute("something-else")]
        Task AnotherRefitMethod();

        [Get(ThisIsDumbButMightHappen.PeopleDoWeirdStuff)]
        Task NoConstantsAllowed();

        [Get  ("spaces-shouldnt-break-me")]
        Task SpacesShouldntBreakMe();

        // We don't need an explicit test for this because if it isn't supported we can't compile
        [Get("anything")]
        Task ReservedWordsForParameterNames(int @int, string @string, float @long);
    }

    public interface IAmNotARefitInterface
    {
        Task NotARefitMethod();
    }

    public interface IBoringCrudApi<T, in TKey> where T : class
    {
        [Post("")]
        Task<T> Create([Body] T paylod);

        [Get("")]
        Task<List<T>> ReadAll();

        [Get("/{key}")]
        Task<T> ReadOne(TKey key);

        [Put("/{key}")]
        Task Update(TKey key, [Body]T payload);

        [Delete("/{key}")]
        Task Delete(TKey key);
    }
}