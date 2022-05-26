using Microsoft.CodeAnalysis;

namespace Refit.Tests
{
    public static partial class CSharpIncrementalSourceGeneratorVerifier<TIncrementalGenerator>
        where TIncrementalGenerator : IIncrementalGenerator, new()
    {
    }
}
