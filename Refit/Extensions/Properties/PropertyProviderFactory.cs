using System;
using System.Collections.Generic;
using System.Reflection;

namespace Refit.Extensions.Properties
{
    public static class PropertyProviderFactory
    {
        /// <summary>
        /// Populates the Refit interface type into HttpRequestMessage.Properties/Options
        /// </summary>
        public static IDictionary<string, object> DefaultPropertyProvider(MethodInfo methodInfo, Type targetType)
        {
            var properties = new Dictionary<string, object> {{HttpRequestMessageOptions.InterfaceType, targetType}};

            return properties;
        }

        public static IDictionary<string, object> NullPropertyProvider(MethodInfo methodInfo, Type targetType)
        {
            return null;
        }

        public static IDictionary<string, object> MethodInfoPropertyProvider(MethodInfo methodInfo, Type targetType)
        {
            var properties = new Dictionary<string, object> {{HttpRequestMessageOptions.MethodInfo, methodInfo}};

            return properties;
        }
    }
}
