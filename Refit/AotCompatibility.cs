using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace Refit
{
    internal static class AotCompatibility
    {
        // Intentionally left blank to avoid changing public API surface (e.g., assembly-level attributes)
        // while keeping a central place for any future AOT-related initializers if needed.
    }
}
