using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nustache;
using Nustache.Core;

namespace Refit.Generator
{
    // * Find all calls to RestService.For<T>, extract all T's
    // * Search for all Interfaces, find the method definitions
    // * Generate the data we need for the template based on interface method
    //   defn's
    // * Get this into an EXE in tools, write a targets file to beforeBuild execute it
    // * Get a props file that adds a dummy file to the project
    // * Write an implementation of RestService that just takes the interface name to
    //   guess the class name based on our template
    //
    // What if the Interface is in another module? (since we copy usings, should be fine)
    // What if the Interface itself is Generic? (fuck 'em)
    public class InterfaceStubGenerator
    {
        public string GenerateInterfaceStubs(string[] paths)
        {
            var trees = paths.Select(x => CSharpSyntaxTree.ParseFile(x)).ToList();
            var interfaceNamesToFind = trees.SelectMany(FindInterfacesToGenerate).Distinct().ToList();

            var interfacesToGenerate = trees.SelectMany(x => x.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>())
                .Where(x => interfaceNamesToFind.Contains(x.Identifier.ValueText))
                .ToList();

            var templateInfo = GenerateTemplateInfoForInterfaceList(interfacesToGenerate);

            Encoders.HtmlEncode = (s) => s;
            var text = Render.StringToString(ExtractTemplateSource(), templateInfo);
            return text;
        }

        public List<string> FindInterfacesToGenerate(SyntaxTree tree)
        {
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

        public TemplateInformation GenerateTemplateInfoForInterfaceList(List<InterfaceDeclarationSyntax> interfaceList)
        {
            var usings = interfaceList
                .SelectMany(interfaceTree => {
                    var rootNode = interfaceTree.Parent;
                    while (rootNode.Parent != null) rootNode = rootNode.Parent;

                    return rootNode.DescendantNodes()
                        .OfType<UsingDirectiveSyntax>()
                        .Select(x => x.Name.ToString());
                })
                .Distinct()
                .Where(x => x != "System" && x != "System.Net.Http")
                .Select(x => new UsingDeclaration() { Item = x });

            var ret = new TemplateInformation() {
                ClassList = interfaceList.Select(x => GenerateClassInfoForInterface(x)).ToList(),
                UsingList = usings.ToList(),
            };

            return ret;
        }

        public ClassTemplateInfo GenerateClassInfoForInterface(InterfaceDeclarationSyntax interfaceTree)
        {
            var ret = new ClassTemplateInfo();
            var parent = interfaceTree.Parent;
            while (parent != null && !(parent is NamespaceDeclarationSyntax)) parent = parent.Parent;

            var ns = parent as NamespaceDeclarationSyntax;
            ret.Namespace = ns.Name.ToString();
            ret.InterfaceName = interfaceTree.Identifier.ValueText;

            ret.MethodList = interfaceTree.Members
                .OfType<MethodDeclarationSyntax>()
                .Select(x => new MethodTemplateInfo() {
                    Name = x.Identifier.ValueText,
                    ReturnType = x.ReturnType.ToString(),
                    ArgumentList = String.Join(",", x.ParameterList.Parameters
                        .Select(y => y.Identifier.ValueText)),
                    ArgumentListWithTypes = String.Join(",", x.ParameterList.Parameters
                        .Select(y => String.Format("{0} {1}", y.Type.ToString(), y.Identifier.ValueText))),
                })
                .ToList();

            return ret;
        }

        public static string ExtractTemplateSource()
        {
            var ourPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Try to return a flat file from the same directory, if it doesn't
            // exist, use the built-in resource version
            if (File.Exists(ourPath)) {
                return File.ReadAllText(Path.Combine(ourPath, "GeneratedInterfaceStubTemplate.cs.mustache"), Encoding.UTF8);
            }

            using (var src = typeof(InterfaceStubGenerator).Assembly.GetManifestResourceStream("Refit.Generator.GeneratedInterfaceStubTemplate.mustache")) {
                var ms = new MemoryStream();
                src.CopyTo(ms);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }

    public class UsingDeclaration
    {
        public string Item { get; set; }
    }

    public class ClassTemplateInfo
    {
        public string Namespace { get; set; }
        public string InterfaceName { get; set; }
        public List<MethodTemplateInfo> MethodList { get; set; }
    }

    public class MethodTemplateInfo
    {
        public string ReturnType { get; set; }
        public string Name { get; set; }
        public string ArgumentListWithTypes { get; set; }
        public string ArgumentList { get; set; }
    }

    public class TemplateInformation
    {
        public List<UsingDeclaration> UsingList { get; set; }
        public List<ClassTemplateInfo> ClassList;
    }
}