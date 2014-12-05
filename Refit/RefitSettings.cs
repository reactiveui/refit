using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Refit
{
    public class RefitSettings
    {
        public RefitSettings()
        {
            UrlParameterFormatter = new DefaultUrlParameterFormatter();
            CustomSerializers = new Dictionary<Type, ITypeSerializer>();
        }

        public IUrlParameterFormatter UrlParameterFormatter { get; set; }

        public Dictionary<Type, ITypeSerializer> CustomSerializers { get; private set; }
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

    public interface ITypeSerializer
    {
        HttpContent SerializeAsHttpContent(object value);
        Task<object> DeserializeFromHttpContent(HttpContent content);
    }
}