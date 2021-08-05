using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Refit.Generator
{
    internal static class ModuleInitializer
    {
        internal static void Initialize()
        {
            try
            {
                var codeAnalysis = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(
                    assembly => assembly.GetName().Name == "Microsoft.CodeAnalysis");
                var codeAnalysisVersion = codeAnalysis.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
                if (!Version.TryParse(codeAnalysisVersion, out var version))
                {
                    return;
                }

                var implementationName = version switch
                {
                    { Major: >= 4 } => "InterfaceStubGenerator.Roslyn40.dll",
                    _ => "InterfaceStubGenerator.Roslyn38.dll",
                };

                if (implementationName is null)
                {
                    return;
                }

                using var implementationStream = typeof(ModuleInitializer).Assembly.GetManifestResourceStream(implementationName);
                using var reader = new BinaryReader(implementationStream);
                Assembly.Load(reader.ReadBytes(int.MaxValue));
            }
            catch
            {
                // Avoid propagating exceptions during assembly load
            }
        }
    }
}
