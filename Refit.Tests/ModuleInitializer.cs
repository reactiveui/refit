using System.Runtime.CompilerServices;
using VerifyTests.DiffPlex;

namespace Refit.Tests;

static class ModuleInitializer
{
    // ModuleInitializer should only be used in apps
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void Init()
    {
        DerivePathInfo((file, _, type, method) => new(Path.Combine(Path.GetDirectoryName(file), "_snapshots"), type.Name, method.Name));

        VerifySourceGenerators.Initialize();
        VerifyDiffPlex.Initialize(OutputType.Compact);
    }
}
