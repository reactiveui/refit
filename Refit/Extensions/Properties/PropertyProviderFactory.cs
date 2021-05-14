using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;

namespace Refit.Extensions.Properties
{
    public static class PropertyProviderFactory
    {
        /// <summary>
        /// Populates the Refit interface type into the <see cref="HttpRequestMessage"/> properties
        /// </summary>
        public static IDictionary<string, object> DefaultPropertyProvider(MethodInfo methodInfo, Type targetType)
        {
            var properties = new Dictionary<string, object> {{HttpRequestMessageOptions.InterfaceType, targetType}};

            return properties;
        }

        /// <summary>
        /// Doesn't populate any properties into the <see cref="HttpRequestMessage"/> properties. Can be used to override the default behavior.
        /// </summary>
        public static IDictionary<string, object> NullPropertyProvider(MethodInfo methodInfo, Type targetType)
        {
            return null;
        }

        /// <summary>
        /// Populates the <see cref="MethodInfo"/> of the currently executing method on the Refit interface into the <see cref="HttpRequestMessage"/> properties
        /// </summary>
        public static IDictionary<string, object> MethodInfoPropertyProvider(MethodInfo methodInfo, Type targetType)
        {
            var properties = new Dictionary<string, object> {{HttpRequestMessageOptions.MethodInfo, methodInfo}};

            return properties;
        }

        /// <summary>
        /// Populates the any custom <see cref="Attribute"/>s present on the currently executing method on the Refit interface into the <see cref="HttpRequestMessage"/> properties
        /// </summary>
        public static IDictionary<string, object> CustomAttributePropertyProvider(MethodInfo methodInfo,
            Type targetType)
        {
            var properties = new Dictionary<string, object>();
            foreach (var attr in methodInfo.GetCustomAttributes())
            {
                if (attr is HttpMethodAttribute ||
                    attr is AuthorizeAttribute ||
                    attr is MultipartAttribute ||
                    attr is PropertyAttribute ||
                    attr is BodyAttribute ||
                    attr is AliasAsAttribute ||
                    attr is AttachmentNameAttribute ||
                    attr is HeaderCollectionAttribute ||
                    attr is HeaderAttribute ||
                    attr is HeadersAttribute ||
                    attr is PropertyAttribute ||
                    attr is QueryAttribute ||
                    attr is QueryUriFormatAttribute)
                {
                    continue;
                }

                properties[attr.GetType().Name] = attr;
            }

            return properties;
        }
    }
}
