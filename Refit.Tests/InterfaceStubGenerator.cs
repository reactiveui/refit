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

            Assert.Contains("IGitHubApi", result);
        }

        [Fact]
        public void FindInterfacesSmokeTest()
        {
            var input = IntegrationTestHelper.GetPath("GitHubApi.cs");
            var fixture = new InterfaceStubGenerator();

            var result = fixture.FindInterfacesToGenerate(CSharpSyntaxTree.ParseText(File.ReadAllText(input)));
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Identifier.ValueText == "IGitHubApi");

            input = IntegrationTestHelper.GetPath("InterfaceStubGenerator.cs");

            result = fixture.FindInterfacesToGenerate(CSharpSyntaxTree.ParseText(File.ReadAllText(input)));
            Assert.Equal(3, result.Count);
            Assert.Contains(result, x => x.Identifier.ValueText == "IAmARefitInterfaceButNobodyUsesMe");
            Assert.Contains(result, x => x.Identifier.ValueText == "IBoringCrudApi");
            Assert.Contains(result, x => x.Identifier.ValueText == "INonGenericInterfaceWithGenericMethod");
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
                .ToLookup(m => m.Identifier.ValueText, fixture.HasRefitHttpMethodAttribute);

            Assert.True(result["RefitMethod"].All(m => m));
            Assert.True(result["AnotherRefitMethod"].All(m => m));
            Assert.False(result["NoConstantsAllowed"].All(m => m));
            Assert.False(result["NotARefitMethod"].All(m => m));
            Assert.True(result["ReadOne"].All(m => m));
            Assert.True(result["SpacesShouldntBreakMe"].All(m => m));
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

            Assert.Equal(13, result.MethodList.Count);
            Assert.Equal("GetUser", result.MethodList[0].Name);
            Assert.Equal("string userName", result.MethodList[0].ArgumentListWithTypes);
            Assert.Equal("IGitHubApi", result.InterfaceName);
            Assert.Equal("IGitHubApi", result.GeneratedClassSuffix);
        }

        [Fact]
        public void GenerateClassInfoForNestedInterfaceSmokeTest()
        {
            var file = CSharpSyntaxTree.ParseText(File.ReadAllText(IntegrationTestHelper.GetPath("GitHubApi.cs")));
            var fixture = new InterfaceStubGenerator();

            var input = file.GetRoot().DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>()
                .First(x => x.Identifier.ValueText == "INestedGitHubApi");

            var result = fixture.GenerateClassInfoForInterface(input);

            Assert.Equal("TestNested.INestedGitHubApi", result.InterfaceName);
            Assert.Equal("TestNestedINestedGitHubApi", result.GeneratedClassSuffix);
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
            Assert.Equal(11, result.ClassList.Count);
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

        [Fact]
        public void GenerateInterfaceStubsWithoutNamespaceSmokeTest()
        {
            var fixture = new InterfaceStubGenerator();

            var result = fixture.GenerateInterfaceStubs(new[] {
                IntegrationTestHelper.GetPath("IServiceWithoutNamespace.cs")
            });

            Assert.Contains("IServiceWithoutNamespace", result);
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

        [Get("spaces-shouldnt-break-me")]
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

    public interface INonGenericInterfaceWithGenericMethod
    {
        [Post("")]
        Task PostMessage<T>([Body] T message) where T : IMessage;

        [Post("")]
        Task PostMessage<T, U, V>([Body] T message, U param1, V param2) where T : IMessage where U : T;
    }

    public interface IMessage { }

}
