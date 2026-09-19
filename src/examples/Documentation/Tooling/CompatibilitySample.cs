// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refit.Documentation.Tooling;

/// <summary>Checks source diagnostics for retained compatibility APIs without suppressing them.</summary>
internal static class CompatibilitySample
{
    /// <summary>The compiler diagnostic retained by warning-level obsolete APIs.</summary>
    private const string ObsoleteWarning = "CS0618";

    /// <summary>Checks the intentional warning on both accessors of the XML DTD opt-out.</summary>
    internal static void CheckXmlDtdWarning()
    {
        const string source = """
            internal static class XmlPolicy
            {
                internal static bool Configure(Refit.XmlReaderWriterSettings settings)
                {
                    settings.AllowDtdProcessing = true;
                    return settings.AllowDtdProcessing;
                }
            }
            """;
        CSharpCompilation compilation = ToolingCompilation.Create(source);
        ToolingCompilation.RequireNoErrors(compilation);
        int warnings = 0;
        foreach (Diagnostic diagnostic in compilation.GetDiagnostics())
        {
            if (diagnostic.Id == ObsoleteWarning)
            {
                warnings++;
            }
        }

        const int accessorCount = 2;
        Check.Require(warnings == accessorCount, "Reading and writing the DTD opt-out both retain their compatibility warning.");
    }

    /// <summary>Checks the exact compiler diagnostic on the obsolete attachment attribute.</summary>
    internal static void CheckAttachmentName()
    {
        const string source = """
            internal interface ILegacyUpload
            {
                [Refit.Multipart]
                [Refit.Post("/form")]
                System.Threading.Tasks.Task UploadAsync([Refit.AttachmentName("sent.bin")] byte[] attachment);
            }
            """;
        CSharpCompilation compilation = ToolingCompilation.Create(source);
        bool warned = AnalyzerSample.Contains(compilation.GetDiagnostics(), ObsoleteWarning);

        ToolingCompilation.RequireNoErrors(compilation);
        Check.Require(warned, "Direct AttachmentName use produces the shipped CS0618 compatibility warning.");
        Console.WriteLine("AttachmentName source probe: CS0618.");
    }

    /// <summary>Checks the default generated-client limitation on multipart form-object flattening.</summary>
    /// <returns>Completion of the actual Refit analyzer execution.</returns>
    internal static async Task CheckFormObjectAsync()
    {
        const string source = """
            internal sealed class FormFields
            {
                public string Name { get; init; } = "Ada";
            }
            internal interface IFormUpload
            {
                [Refit.Multipart]
                [Refit.Post("/form")]
                System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> UploadAsync([Refit.FormObject] FormFields fields);
            }
            """;
        CSharpCompilation compilation = ToolingCompilation.Create(source);
        ImmutableArray<Diagnostic> diagnostics = await AnalyzerSample.DiagnoseAsync(compilation);
        bool requiresReflection = AnalyzerSample.Contains(diagnostics, "RF006");

        ToolingCompilation.RequireNoErrors(compilation);
        Check.Require(requiresReflection, "Default generated-client FormObject use reports RF006.");
        Console.WriteLine("FormObject default analyzer probe: RF006.");
    }

    /// <summary>Checks the error-level obsolete constructor on the retained JSON serializer.</summary>
    internal static void CheckJsonContentSerializer()
    {
        const string source = """
            internal static class LegacySerializer
            {
                internal static object Create() => new Refit.JsonContentSerializer();
            }
            """;
        CSharpCompilation compilation = ToolingCompilation.Create(source);
        bool rejected = AnalyzerSample.Contains(compilation.GetDiagnostics(), "CS0619");

        Check.Require(rejected, "Direct JsonContentSerializer construction produces the shipped CS0619 compatibility error.");
        Console.WriteLine("JsonContentSerializer source probe: CS0619.");
    }

    /// <summary>Checks the warning on the retained Json body-serialization enum member.</summary>
    internal static void CheckJsonBodyMode()
    {
        const string source = """
            internal static class LegacyBodyMode
            {
                internal static Refit.BodySerializationMethod Mode => Refit.BodySerializationMethod.Json;
            }
            """;
        CSharpCompilation compilation = ToolingCompilation.Create(source);
        bool warned = AnalyzerSample.Contains(compilation.GetDiagnostics(), ObsoleteWarning);
        ToolingCompilation.RequireNoErrors(compilation);
        Check.Require(warned, "Direct Json body mode use produces the shipped CS0618 compatibility warning.");
        Console.WriteLine("BodySerializationMethod.Json source probe: CS0618.");
    }
}
