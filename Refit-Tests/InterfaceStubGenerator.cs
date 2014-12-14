using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using Nustache;
using Nustache.Core;
using Refit; // InterfaceStubGenerator looks for this
using Refit.Generator;

namespace Refit.Tests
{
    [TestFixture]
    public class InterfaceStubGeneratorTests
    {
        [Test]
        public void GenerateInterfaceStubsSmokeTest()
        {
            var fixture = new InterfaceStubGenerator();

            var result = fixture.GenerateInterfaceStubs(new[] {
                IntegrationTestHelper.GetPath("RestService.cs"),
                IntegrationTestHelper.GetPath("GitHubApi.cs"),
            });

            Assert.True(result.Contains("IGitHubApi"));
        }

        [Test]
        public void FindInterfacesSmokeTest()
        {
            var input = IntegrationTestHelper.GetPath("GitHubApi.cs");
            var fixture = new InterfaceStubGenerator();

            var result = fixture.FindInterfacesToGenerate(CSharpSyntaxTree.ParseFile(input));
            Assert.AreEqual(1, result.Count);
            Assert.True(result.Any(x => x.Identifier.ValueText == "IGitHubApi"));

            input = IntegrationTestHelper.GetPath("InterfaceStubGenerator.cs");

            result = fixture.FindInterfacesToGenerate(CSharpSyntaxTree.ParseFile(input));
            Assert.AreEqual(2, result.Count);
            Assert.True(result.Any(x => x.Identifier.ValueText == "IAmARefitInterfaceButNobodyUsesMe"));
            Assert.True(result.Any(x => x.Identifier.ValueText == "IBoringCrudApi"));
            Assert.True(result.All(x => x.Identifier.ValueText != "IAmNotARefitInterface"));
        }

        [Test]
        public void HasRefitHttpMethodAttributeSmokeTest()
        {
            var file = CSharpSyntaxTree.ParseFile(IntegrationTestHelper.GetPath("InterfaceStubGenerator.cs"));
            var fixture = new InterfaceStubGenerator();

            var input = file.GetRoot().DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>()
                .SelectMany(i => i.Members.OfType<MethodDeclarationSyntax>())
                .ToList();

            var result = input
                .ToDictionary(m => m.Identifier.ValueText, fixture.HasRefitHttpMethodAttribute);

            Assert.IsTrue(result["RefitMethod"]);
            Assert.IsTrue(result["AnotherRefitMethod"]);
            Assert.IsFalse(result["NoConstantsAllowed"]);
            Assert.IsFalse(result["NotARefitMethod"]);
            Assert.IsTrue(result["ReadOne"]);
            Assert.IsTrue(result["SpacesShouldntBreakMe"]);
        }

        [Test]
        public void GenerateClassInfoForInterfaceSmokeTest()
        {
            var file = CSharpSyntaxTree.ParseFile(IntegrationTestHelper.GetPath("GitHubApi.cs"));
            var fixture = new InterfaceStubGenerator();

            var input = file.GetRoot().DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>()
                .First(x => x.Identifier.ValueText == "IGitHubApi");

            var result = fixture.GenerateClassInfoForInterface(input);

            Assert.AreEqual(8, result.MethodList.Count);
            Assert.AreEqual("GetUser", result.MethodList[0].Name);
            Assert.AreEqual("string userName", result.MethodList[0].ArgumentListWithTypes);
        }

        [Test]
        public void GenerateTemplateInfoForInterfaceListSmokeTest()
        {
            var file = CSharpSyntaxTree.ParseFile(IntegrationTestHelper.GetPath("RestService.cs"));
            var fixture = new InterfaceStubGenerator();

            var input = file.GetRoot().DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>()
                .ToList();

            var result = fixture.GenerateTemplateInfoForInterfaceList(input);
            Assert.AreEqual(6, result.ClassList.Count);
        }

        [Test]
        public void RetainsAliasesInUsings()
        {
            var fixture = new InterfaceStubGenerator();

            var syntaxTree = CSharpSyntaxTree.ParseFile(IntegrationTestHelper.GetPath("NamespaceCollisionApi.cs"));
            var interfaceDefinition = syntaxTree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            var result = fixture.GenerateTemplateInfoForInterfaceList(new List<InterfaceDeclarationSyntax>(interfaceDefinition));

            var usingList = result.UsingList.Select(x => x.Item).ToList();
            CollectionAssert.Contains(usingList, "SomeType = CollisionA.SomeType");
            CollectionAssert.Contains(usingList, "CollisionB");
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