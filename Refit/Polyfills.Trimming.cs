#if NETSTANDARD2_0 || NET462
namespace System.Diagnostics.CodeAnalysis
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Constructor | System.AttributeTargets.Method | System.AttributeTargets.Property | System.AttributeTargets.Event, Inherited = false)]
    internal sealed class RequiresUnreferencedCodeAttribute : System.Attribute
    {
        public RequiresUnreferencedCodeAttribute(string message) => Message = message;
        public string Message { get; }
        public string? Url { get; set; }
    }

    [System.AttributeUsage(System.AttributeTargets.Constructor | System.AttributeTargets.Method, AllowMultiple = true)]
    internal sealed class DynamicDependencyAttribute : System.Attribute
    {
        public DynamicDependencyAttribute(DynamicallyAccessedMemberTypes memberTypes, System.Type type) { }
        public DynamicDependencyAttribute(string memberSignature, System.Type type) { }
    }
}
#endif
