using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Refit
{
    public class RestMethodInfo
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public string RelativePath { get; set; }
        public Dictionary<int, string> ParameterMap { get; set; }
        public Tuple<BodySerializationMethod, int> BodyParameterInfo { get; set; }
        public Dictionary<int, string> QueryParameterMap { get; set; }
        public Type ReturnType { get; set; }
        public Type SerializedReturnType { get; set; }

        public RestMethodInfo(Type targetInterface, MethodInfo methodInfo)
        {
            Type = targetInterface;
            Name = methodInfo.Name;
            MethodInfo = methodInfo;
        }
    }
}
