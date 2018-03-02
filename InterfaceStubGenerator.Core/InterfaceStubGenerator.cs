using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nustache.Core;

namespace Refit.Generator
{
    // * Search for all Interfaces, find the method definitions 
    //   and make sure there's at least one Refit attribute on one
    // * Generate the data we need for the template based on interface method
    //   defn's
    // * Get this into an EXE in tools, write a targets file to beforeBuild execute it
    // * Get a props file that adds a dummy file to the project
    // * Write an implementation of RestService that just takes the interface name to
    //   guess the class name based on our template
    //
    // What if the Interface is in another module? (since we copy usings, should be fine)
    public class InterfaceStubGenerator
    {
        static readonly HashSet<string> httpMethodAttributeNames = new HashSet<string>(
            new[] { "Get", "Head", "Post", "Put", "Delete", "Patch" }
                .SelectMany(x => new[] { "{0}", "{0}Attribute" }.Select(f => string.Format(f, x))));

        public InterfaceStubGenerator() : this(null) { }

        public InterfaceStubGenerator(Action<string> logWarning)
        {
            Log = logWarning;
        }

        public Action<string> Log { get; }

        public static string ExtractTemplateSource()
        {
            var ourPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GeneratedInterfaceStubTemplate.mustache");

            // Try to return a flat file from the same directory, if it doesn't
            // exist, use the built-in resource version
            if (File.Exists(ourPath))
            {
                return File.ReadAllText(ourPath, Encoding.UTF8);
            }

            using (var src = typeof(InterfaceStubGenerator).Assembly.GetManifestResourceStream("Refit.Generator.GeneratedInterfaceStubTemplate.mustache"))
            {
                var ms = new MemoryStream();
                src.CopyTo(ms);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public List<TypeDeclarationSyntax> FindInterfacesToGenerate(SyntaxTree tree)
        {
            var nodes = tree.GetRoot().DescendantNodes().ToList();

            // Make sure this file imports Refit. If not, we're not going to 
            // find any Refit interfaces
            // NB: This falls down in the tests unless we add an explicit "using Refit;",
            // but we can rely on this being there in any other file
            if (nodes.OfType<UsingDirectiveSyntax>().All(u => u.Name.ToFullString() != "Refit"))
                return new List<TypeDeclarationSyntax>();

            var modifiers = new List<SyntaxToken>();
            modifiers.Add(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));

            

            var abstracts = nodes.OfType<ClassDeclarationSyntax>()
                .Where(c => c.Modifiers.Select(m => m.ValueText).Contains("abstract"))
                .Where(i => i.Members.OfType<MethodDeclarationSyntax>().Any(HasRefitHttpMethodAttribute))
                .Cast<TypeDeclarationSyntax>();



            var interfaces = nodes.OfType<InterfaceDeclarationSyntax>()
                .Where(i => i.Members.OfType<MethodDeclarationSyntax>().Any(HasRefitHttpMethodAttribute))
                .Cast<TypeDeclarationSyntax>();

            return abstracts.Concat(interfaces).ToList();

            
        }

        public ClassTemplateInfo GenerateClassInfoForInterface(TypeDeclarationSyntax interfaceTree)
        {
            var ret = new ClassTemplateInfo();
            var parent = interfaceTree.Parent;
            while (parent != null && !(parent is NamespaceDeclarationSyntax)) parent = parent.Parent;

            var ns = parent as NamespaceDeclarationSyntax;
            ret.Namespace = ns.Name.ToString();
            ret.InterfaceName = GetInterfaceName(interfaceTree.Identifier);
            ret.GeneratedClassSuffix = ret.InterfaceName.Replace(".", "");
            ret.Modifiers = interfaceTree.Modifiers.Select(t => t.ValueText).FirstOrDefault(m => m == "public" || m == "internal");

            if (interfaceTree.TypeParameterList != null)
            {
                var typeParameters = interfaceTree.TypeParameterList.Parameters;
                if (typeParameters.Any())
                {
                    ret.TypeParameters = string.Join(", ", typeParameters.Select(p => p.Identifier.ValueText));
                }

                ret.ConstraintClauses = interfaceTree.ConstraintClauses.ToFullString().Trim();
            }

            ret.MethodList = interfaceTree.Members
                                          .OfType<MethodDeclarationSyntax>()
                                          .Select(x =>
                                          {
                                              var mti = new MethodTemplateInfo
                                              {
                                                  Name = x.Identifier.Text,
                                                  ReturnType = x.ReturnType.ToString(),
                                                  ArgumentList = string.Join(",",
                                                                             x.ParameterList.Parameters
                                                                              .Select(y => y.Identifier.Text)),
                                                  ArgumentListWithTypes = string.Join(",",
                                                                                      x.ParameterList.Parameters
                                                                                       .Select(y => $"{y.Type.ToString()} {y.Identifier.Text}")),
                                                  ArgumentTypesList = string.Join(",", x.ParameterList.Parameters
                                                                                        .Select(y => $"typeof({y.Type.ToString()})")),
                                                  IsRefitMethod = HasRefitHttpMethodAttribute(x),
                                                  FromAbstractClass = interfaceTree is ClassDeclarationSyntax,
                                                  FromInterface = interfaceTree is InterfaceDeclarationSyntax
                                              };

                                              
                                              if (x.TypeParameterList != null)
                                              {
                                                  var typeParameters = x.TypeParameterList.Parameters;
                                                  if (typeParameters.Any())
                                                  {
                                                      mti.MethodTypeParameters = string.Join(", ", typeParameters.Select(p => p.Identifier.ValueText));
                                                      mti.MethodTypeParameterList = string.Join(", ", typeParameters.Select(p => $"typeof({p.Identifier.ValueText})"));
                                                      mti.MethodTypeParameterNames = $"{string.Join(", ", typeParameters.Select(p => $"{{typeof({p.Identifier.ValueText}).AssemblyQualifiedName}}"))}";
                                                  }
                                                  mti.MethodConstraintClauses = x.ConstraintClauses.ToFullString().Trim();
                                              }

                                              if (mti.FromAbstractClass) {
                                                  mti.Modifier = BuildModifierForAbstractMethod(x, mti.IsRefitMethod);
                                              }
                                              
                                              if (mti.FromInterface) {
                                                  mti.Modifier = "public";
                                              }
                                              
                                              var b = $"base.{mti.Name}({mti.ArgumentList});";
                                              
                                              if (ModifiersContains(x.Modifiers, "async"))
                                              {
                                                  mti.Modifier += " async";
                                                  b = $"await {b}";
                                              }

                                              if (mti.ReturnType != "void")
                                              {
                                                  b = $"return {b}";
                                              }

                                              //mti.InnerCode = mti.FromAbstractClass ? b : "throw new NotImplementedException(\"Either this method has no Refit HTTP method attribute or you\'ve used something other than a string literal for the \'path\' argument.\");";
                                              
                                              mti.InnerCode = x.Body?.Statements.ToFullString().Trim() ?? "throw new NotImplementedException(\"Either this method has no Refit HTTP method attribute or you\'ve used something other than a string literal for the \'path\' argument.\");";

                                              return mti;
                                          })
                                          .ToList();

            return ret;
        }

        private string BuildModifierForAbstractMethod(MethodDeclarationSyntax methodDeclarationSyntax, bool isRefitMethod)
        {
            var modifier = GetAccessModifier(methodDeclarationSyntax);

            if (!isRefitMethod)
            {
                modifier += " new";
            }

            modifier += methodDeclarationSyntax.Modifiers.Any(m => m.ValueText == "abstract") ? " override" : " virtual";

            return modifier;
        }

        private string GetAccessModifier(MethodDeclarationSyntax methodDeclarationSyntax)
        {

            var modifiers = methodDeclarationSyntax.Modifiers;

            var str = string.Empty;

            if (ModifiersContains(modifiers, "public"))
            {
                str = "public";
            }

            if (ModifiersContains(modifiers, "protected"))
            {
                str += " protected";
            }

            if (ModifiersContains(modifiers, "internal"))
            {
                str += " internal";
            }
            

            return str.Trim();
        }

        private bool ModifiersContains(SyntaxTokenList modifiers, string modifier)
        {
            return modifiers.Select(m => m.ValueText).Contains(modifier);
        }


        public string GenerateInterfaceStubs(string[] paths)
        {
            var trees = paths.Select(x => CSharpSyntaxTree.ParseText(File.ReadAllText(x))).ToList();

            var interfacesToGenerate = trees.SelectMany(FindInterfacesToGenerate).OrderBy(i => i.Identifier.Text).ToList();

            var templateInfo = GenerateTemplateInfoForInterfaceList(interfacesToGenerate);

            GenerateWarnings(interfacesToGenerate);

            Encoders.HtmlEncode = s => s;
            var text = Render.StringToString(ExtractTemplateSource(), templateInfo);
            return text;
        }

        public TemplateInformation GenerateTemplateInfoForInterfaceList(List<TypeDeclarationSyntax> interfaceList)
        {
            var usings = interfaceList
                         .SelectMany(interfaceTree =>
                         {
                             var rootNode = interfaceTree.Parent;
                             while (rootNode.Parent != null) rootNode = rootNode.Parent;

                             return rootNode.DescendantNodes()
                                            .OfType<UsingDirectiveSyntax>()
                                            .Select(x => $"{x.Alias} {x.StaticKeyword} {x.Name}".TrimStart());
                         })
                         .Distinct()
                         .Where(x => x != "System" && x != "System.Net.Http" && x != "System.Collections.Generic" && x != "System.Linq")
                         .Select(x => new UsingDeclaration { Item = x });

            var ret = new TemplateInformation
            {
                ClassList = interfaceList.Select(GenerateClassInfoForInterface).ToList(),
                UsingList = usings.ToList()
            };

            return ret;
        }

        public void GenerateWarnings(List<TypeDeclarationSyntax> interfacesToGenerate)
        {
            var missingAttributeWarnings = interfacesToGenerate
                                           .SelectMany(i => i.Members.OfType<MethodDeclarationSyntax>().Select(m => new
                                           {
                                               Interface = i,
                                               Method = m
                                           }))
                                           .Where(x => !HasRefitHttpMethodAttribute(x.Method))
                                           .Select(x => new MissingRefitAttributeWarning(x.Interface, x.Method));


            var diagnostics = missingAttributeWarnings;

            foreach (var diagnostic in diagnostics)
            {
                Log?.Invoke(diagnostic.ToString());
            }
        }

        public bool HasRefitHttpMethodAttribute(MethodDeclarationSyntax method)
        {
            // We could also verify that the single argument is a string, 
            // but what if somebody is dumb and uses a constant?
            // Could be turtles all the way down.
            return method.AttributeLists.SelectMany(a => a.Attributes)
                         .Any(a => httpMethodAttributeNames.Contains(a.Name.ToString().Split('.').Last()) &&
                                   a.ArgumentList.Arguments.Count == 1 &&
                                   a.ArgumentList.Arguments[0].Expression.Kind() == SyntaxKind.StringLiteralExpression);
        }

        private string GetInterfaceName(SyntaxToken identifier)
        {
            if (identifier == null) return "";
            var interfaceParent = identifier.Parent != null ? identifier.Parent.Parent : identifier.Parent;

            if ((interfaceParent as ClassDeclarationSyntax) != null)
            {
                var classParent = (interfaceParent as ClassDeclarationSyntax).Identifier;
                return classParent + "." + identifier.ValueText;
            }

            return identifier.ValueText;
        }
    }

    public class UsingDeclaration
    {
        public string Item { get; set; }
    }

    public class ClassTemplateInfo
    {
        public string ConstraintClauses { get; set; }
        public string GeneratedClassSuffix { get; set; }
        public string InterfaceName { get; set; }
        public List<MethodTemplateInfo> MethodList { get; set; }
        public string Modifiers { get; set; }
        public string Namespace { get; set; }
        public string TypeParameters { get; set; }
    }

    public class MethodTemplateInfo
    {
        public string ArgumentList { get; set; }
        public string ArgumentListWithTypes { get; set; }
        public string ArgumentTypesList { get; set; }
        public bool IsRefitMethod { get; set; }
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public string MethodTypeParameters { get; set; }
        public string MethodConstraintClauses { get; set; }
        public string MethodTypeParameterList { get; set; }
        public string MethodTypeParameterNames { get; set; }
        public string Modifier { get; set; }
        public string AccessModifier { get; set; }
        public string InnerCode { get; set; }
        public bool FromAbstractClass { get; set; }
        public bool FromInterface { get; set; }
    }

    public class TemplateInformation
    {
        public List<ClassTemplateInfo> ClassList;
        public List<UsingDeclaration> UsingList { get; set; }
    }
}
