using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace Refit.Extensions.Properties
{
    public static class PropertyProviderFactory
    {
        /// <summary>
        /// Populates the Refit interface type into the <see cref="HttpRequestMessage"/> properties
        /// </summary>
        public static IDictionary<string, object> RefitInterfaceTypePropertyProvider(MethodInfo methodInfo, Type targetType)
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
        /// Populates any custom <see cref="Attribute"/> present on the currently executing method on the Refit interface that is not a subclass of <see cref="RefitAttribute"/>
        /// into the <see cref="HttpRequestMessage"/> properties with the key as the Name property on the <see cref="Type"/> of the <see cref="Attribute"/>
        /// </summary>
        public static IDictionary<string, object> CustomAttributePropertyProvider(MethodInfo methodInfo,
            Type targetType)
        {
            var properties = new Dictionary<string, object>();

            foreach (var interfaceAttribute in targetType.GetCustomAttributes())
            {
                //just select custom attributes where not this type
                if (interfaceAttribute is RefitAttribute)
                {
                    continue;
                }

                properties[interfaceAttribute.GetType().Name] = interfaceAttribute;
            }

            foreach (var methodAttribute in methodInfo.GetCustomAttributes())
            {
                if (methodAttribute is RefitAttribute)
                {
                    continue;
                }

                properties[methodAttribute.GetType().Name] = methodAttribute;
            }

            return properties;
        }
    }
}
