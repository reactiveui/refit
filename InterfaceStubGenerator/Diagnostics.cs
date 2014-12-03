using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refit.Generator
{
    public class DiagnosticsLogger
    {
        List<Diagnostic> diagnostics = new List<Diagnostic>();

        public IEnumerable<Diagnostic> Diagnostics { get { return diagnostics.AsReadOnly(); } }

        public void Add(Diagnostic diagnostic)
        {
            this.diagnostics.Add(diagnostic);
        }

        public void AddRange(IEnumerable<Diagnostic> collection)
        {
            this.diagnostics.AddRange(collection);
        }

        public void Dump()
        {
            var builder = new StringBuilder();

            foreach (var diagnostic in diagnostics)
            {
                builder.Clear();

                if (!string.IsNullOrWhiteSpace(diagnostic.File))
                {
                    builder.Append(diagnostic.File);
                    if (diagnostic.Line.HasValue)
                    {
                        builder.AppendFormat("({0}", diagnostic.Line);
                        if (diagnostic.Character.HasValue)
                            builder.AppendFormat(",{0}", diagnostic.Character);
                        builder.Append(")");
                    }
                    builder.Append(": ");
                }
                builder.AppendFormat("{0} {1}", diagnostic.Type, diagnostic.Code);
                if (!string.IsNullOrWhiteSpace(diagnostic.Message))
                    builder.AppendFormat(": {0}", diagnostic.Message);

                Console.Error.WriteLine(builder.ToString());
            }
        }
    }

    public class Diagnostic
    {
        public string Type { get; private set; }
        public string Code { get; private set; }
        public string File { get; protected set; }
        public int? Line { get; protected set; }
        public int? Character { get; protected set; }
        public string Message { get; protected set; }

        public Diagnostic(string type, string code)
        {
            this.Type = type;
            this.Code = code;
        }
    }

    public class Warning : Diagnostic
    {
        public Warning(string code) : base("warning", code) { }
    }

    public class MissingRefitAttributeWarning : Warning
    {
        public string InterfaceName { get; private set; }
        public string MethodName { get; private set; }

        public MissingRefitAttributeWarning(InterfaceDeclarationSyntax @interface, MethodDeclarationSyntax method)
            : base("RF001")
        {
            var location = method.GetLocation();
            var line = location.GetMappedLineSpan().StartLinePosition;

            this.File = location.FilePath;
            this.Line = line.Line + 1;
            this.Character = line.Character + 1;
            this.InterfaceName = @interface.Identifier.Text;
            this.MethodName = method.Identifier.Text;

            this.Message = string.Format(
                "Method {0}.{1} either has no Refit HTTP method attribute or you've used something other than a string literal for the 'path' argument.",
                this.InterfaceName, this.MethodName);
        }
    }
}
