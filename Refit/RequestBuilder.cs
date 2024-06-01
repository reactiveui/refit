using System.Net.Http;

namespace Refit
{
    /// <summary>
    /// IRequestBuilder.
    /// </summary>
    public interface IRequestBuilder
    {
        /// <summary>
        /// Builds the rest result function for method.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <param name="genericArgumentTypes">The generic argument types.</param>
        /// <returns></returns>
        Func<HttpClient, object[], object?> BuildRestResultFuncForMethod(
            string methodName,
            Type[]? parameterTypes = null,
            Type[]? genericArgumentTypes = null
        );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRequestBuilder<T> : IRequestBuilder { }

    /// <summary>
    /// RequestBuilder.
    /// </summary>
    public static class RequestBuilder
    {
        static readonly RequestBuilderFactory PlatformRequestBuilderFactory = new();

        /// <summary>
        /// Fors the type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="settings">The settings.</param>
        /// <returns></returns>
        public static IRequestBuilder<T> ForType<T>(RefitSettings? settings) => PlatformRequestBuilderFactory.Create<T>(settings);

        /// <summary>
        /// Fors the type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IRequestBuilder<T> ForType<T>() => PlatformRequestBuilderFactory.Create<T>(null);

        /// <summary>
        /// Fors the type.
        /// </summary>
        /// <param name="refitInterfaceType">Type of the refit interface.</param>
        /// <param name="settings">The settings.</param>
        /// <returns></returns>
        public static IRequestBuilder ForType(Type refitInterfaceType, RefitSettings? settings) => PlatformRequestBuilderFactory.Create(refitInterfaceType, settings);

        /// <summary>
        /// Fors the type.
        /// </summary>
        /// <param name="refitInterfaceType">Type of the refit interface.</param>
        /// <returns></returns>
        public static IRequestBuilder ForType(Type refitInterfaceType) => PlatformRequestBuilderFactory.Create(refitInterfaceType, null);
    }
}
