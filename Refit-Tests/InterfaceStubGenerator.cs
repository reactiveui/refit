using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace Refit.Tests
{
    // * Find all calls to RestService.For<T>, extract all T's
    // * Search for all Interfaces, find the method definitions
    // * Generate the data we need for the template based on interface method 
    //   defn's
    //
    // What if the Interface is in another module? (fuck 'em)
    // What if the Interface itself is Generic? (fuck 'em)
    public class InterfaceStubGenerator
    {
        public List<string> FindInterfacesToGenerate(string path)
        {
            var tree = CSharpSyntaxTree.ParseFile(path);

            var restServiceCalls = tree.GetRoot().DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .Where(x => x.Expression is IdentifierNameSyntax &&
                    ((IdentifierNameSyntax)x.Expression).Identifier.ValueText == "RestService" &&
                    x.Name.Identifier.ValueText == "For");

            return restServiceCalls
                .SelectMany(x => ((GenericNameSyntax)x.Name).TypeArgumentList.Arguments)
                .Select(x => ((IdentifierNameSyntax)x).Identifier.ValueText)
                .Distinct()
                .ToList();
        }
    }

    public class InterfaceStubGeneratorTests
    {
        [Test]
        public void FindInterfacesSmokeTest()
        {
            var input = IntegrationTestHelper.GetPath("RestService.cs");
            var fixture = new InterfaceStubGenerator();

            var result = fixture.FindInterfacesToGenerate(input);
            Assert.AreEqual(2, result.Count);
            Assert.True(result.Any(x => x == "IGitHubApi"));
        }
    }
}
