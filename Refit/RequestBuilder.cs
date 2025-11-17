using System.Net.Http;
#if NET10_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Refit
{
    public interface IRequestBuilder
    {
        Func<HttpClient, object[], object?> BuildRestResultFuncForMethod(
            string methodName,
            Type[]? parameterTypes = null,
            Type[]? genericArgumentTypes = null
        );
    }

    public interface IRequestBuilder<T> : IRequestBuilder { }

    public static class RequestBuilder
    {
        static readonly RequestBuilderFactory PlatformRequestBuilderFactory = new();

#if NET10_0_OR_GREATER
        public static IRequestBuilder<T> ForType< [
            DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.None |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods
            )] T >(RefitSettings? settings) =>
            PlatformRequestBuilderFactory.Create<T>(settings);
#else
        public static IRequestBuilder<T> ForType<T>(RefitSettings? settings) =>
            PlatformRequestBuilderFactory.Create<T>(settings);
#endif

#if NET10_0_OR_GREATER
        public static IRequestBuilder<T> ForType< [
            DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.None |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods
            )] T >() =>
            PlatformRequestBuilderFactory.Create<T>(null);
#else
        public static IRequestBuilder<T> ForType<T>() =>
            PlatformRequestBuilderFactory.Create<T>(null);
#endif

#if NET10_0_OR_GREATER
        [RequiresUnreferencedCode("Refit uses reflection to analyze interface methods. Ensure referenced interfaces and DTOs are preserved when trimming.")]
        public static IRequestBuilder ForType(
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.None |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods
            )] Type refitInterfaceType,
            RefitSettings? settings
        )
#else
        public static IRequestBuilder ForType(
            Type refitInterfaceType,
            RefitSettings? settings
        )
#endif
        {
            return new CachedRequestBuilderImplementation(
                new RequestBuilderImplementation(refitInterfaceType, settings)
            );
        }

#if NET10_0_OR_GREATER
        [RequiresUnreferencedCode("Refit uses reflection to analyze interface methods. Ensure referenced interfaces and DTOs are preserved when trimming.")]
#endif
        public static IRequestBuilder ForType(Type refitInterfaceType) =>
            ForType(refitInterfaceType, null);
    }
}
