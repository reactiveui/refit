using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
namespace Refit
{
    public class RefitSettings
    {
        public RefitSettings()
        {
            UrlParameterFormatter = new DefaultUrlParameterFormatter();
        }
        public JsonSerializerSettings JsonSerializerSettings { get; set; }
        public IUrlParameterFormatter UrlParameterFormatter { get; set; }
    }

    public interface IUrlParameterFormatter
    {
        string Format(object value, ParameterInfo parameterInfo);
    }

    public class DefaultUrlParameterFormatter : IUrlParameterFormatter
    {
        public virtual string Format(object parameterValue, ParameterInfo parameterInfo)
        {
            return parameterValue != null ? parameterValue.ToString() : null;
        }
    }
}
