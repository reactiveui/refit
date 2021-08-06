using System;

using Microsoft.CodeAnalysis;

namespace Refit.Generator
{
    public abstract class AbstractInterfaceStubGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            throw new NotImplementedException();
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            throw new NotImplementedException();
        }
    }
}
