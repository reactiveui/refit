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
using Refit.Generator;

namespace Refit.Tests
{
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
            var input = IntegrationTestHelper.GetPath("RestService.cs");
            var fixture = new InterfaceStubGenerator();

            var result = fixture.FindInterfacesToGenerate(CSharpSyntaxTree.ParseFile(input));
            Assert.AreEqual(2, result.Count);
            Assert.True(result.Any(x => x == "IGitHubApi"));
        }

        [Test]
        public void GenerateClassInfoForInterfaceSmokeTest()
        {
            var file = CSharpSyntaxTree.ParseFile(IntegrationTestHelper.GetPath("RestService.cs"));
            var fixture = new InterfaceStubGenerator();

            var input = file.GetRoot().DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>()
                .First(x => x.Identifier.ValueText == "IGitHubApi");

            var result = fixture.GenerateClassInfoForInterface(input);

            Assert.AreEqual(2, result.MethodList.Count);
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
            Assert.AreEqual(2, result.ClassList.Count);
        }
    }
}