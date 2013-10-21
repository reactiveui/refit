using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Refit.AspNet
{
    public class RestMethodResolver : IRestMethodResolver
    {
        public Dictionary<string, RestMethodInfo> GetInterfaceRestMethodInfo(Type targetInterface)
        {
            return targetInterface.GetMethods()
                .SelectMany(x =>
                {
                    var attrs = x.GetCustomAttributes(true);
                    var hasRoutingAttribute = attrs.OfType<RouteAttribute>().Any();
                    if (!hasRoutingAttribute) return Enumerable.Empty<RestMethodInfo>();

                    return EnumerableEx.Return(buildRestMethodInfo(targetInterface, x));
                })
                .ToDictionary(k => k.Name, v => v);
        }

        public RestMethodInfo buildRestMethodInfo(Type targetInterface, MethodInfo methodInfo)
        {
            var restMethodInfo = new RestMethodInfo(targetInterface, methodInfo);

            // Do more work.

            return restMethodInfo;
        }
    }
}
