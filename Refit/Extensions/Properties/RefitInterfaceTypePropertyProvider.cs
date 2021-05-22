using System;
using System.Collections.Generic;
using System.Reflection;

namespace Refit.Extensions.Properties
{
    public class RefitInterfaceTypePropertyProvider : PropertyProvider
    {
        public void ProvideProperties(IDictionary<string, object> properties, MethodInfo methodInfo, Type refitInterfaceType)
        {
            properties[HttpRequestMessageOptions.InterfaceType] = refitInterfaceType;
        }
    }
}
