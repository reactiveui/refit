using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Refit.Generator
{
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
            Type = type;
            Code = code;
        }

        protected void setLocation(Location location)
        {
            var line = location.GetMappedLineSpan().StartLinePosition;

            File = location.GetMappedLineSpan().Path;
            Line = line.Line + 1;
            Character = line.Character + 1;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(File)) {
                builder.Append(File);
                if (Line.HasValue) {
                    builder.AppendFormat("({0}", Line);
                    if (Character.HasValue)
                        builder.AppendFormat(",{0}", Character);
                    builder.Append(")");
                }
                builder.Append(": ");
            }
            builder.AppendFormat("{0} {1}", Type, Code);
            if (!string.IsNullOrWhiteSpace(Message))
                builder.AppendFormat(": {0}", Message);

            return builder.ToString();
        }
    }

    public class Warning : Diagnostic
    {
        public Warning(string code) : base("warning", code) { }
    }

    public class Error : Diagnostic
    {
        public Error(string code) : base("error", code) { }
    }


    public class MissingRefitAttributeWarning : Warning
    {
        public string InterfaceName { get; private set; }
        public string MethodName { get; private set; }

        public MissingRefitAttributeWarning(InterfaceDeclarationSyntax @interface, MethodDeclarationSyntax method)
            : base("RF001")
        {
            setLocation(method.GetLocation());

            InterfaceName = @interface.Identifier.Text;
            MethodName = method.Identifier.Text;

            Message = string.Format(
                "Method {0}.{1} either has no Refit HTTP method attribute or you've used something other than a string literal for the 'path' argument.",
                InterfaceName, MethodName);
        }
    }

    public class MultipleRefitMethodSameNameWarning : Warning
    {
        public string InterfaceName { get; private set; }
        public string MethodName { get; private set; }

        public MultipleRefitMethodSameNameWarning(InterfaceDeclarationSyntax @interface, MethodDeclarationSyntax method)
            : base("RF002")
        {
            setLocation(method.GetLocation());

            InterfaceName = @interface.Identifier.Text;
            MethodName = method.Identifier.Text;

            Message = string.Format(
                "Method {0}.{1} has been declared multiple times. Refit doesn't support overloading.",
                InterfaceName, MethodName);
        }
    }

    public class ReadOnlyFileError : Error
    {
        public ReadOnlyFileError(FileInfo file) : base("RF003")
        {
            File = file.FullName;
            Message = "File is marked as read-only and is not up-to-date.";
        }
    }
}
