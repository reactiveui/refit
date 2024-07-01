using System.Runtime.CompilerServices;

namespace RefitGenerator.Tests;

public class BaseTestClass
{
    static BaseTestClass()
    {
        Verifier.DerivePathInfo(
            (file, _, type, method) => new(Path.Combine(Path.GetDirectoryName(file), "_snapshots"), type.Name, method.Name)
        );

        VerifySourceGenerators.Initialize();
        VerifyDiffPlex.Initialize(VerifyTests.DiffPlex.OutputType.Compact);
    }
}
