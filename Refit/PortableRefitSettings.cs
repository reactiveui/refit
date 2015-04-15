using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Refit
{
    public class RefitSettings
    {
        public RefitSettings()
        {
        }
        public JsonSerializerSettings JsonSerializerSettings { get; set; }
        public IUrlParameterFormatter UrlParameterFormatter { get; set; }
        public IUriTemplateHandler UriTemplateHandler { get; set; }
    }

    public interface IUriTemplateHandler
    {
        Uri GetRequestUri(IUrlParameterFormatter urlParameterFormatter, RestMethodInfo restMethod, object[] paramList, string basePath = "");   
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

    public class DefaultUriTemplateHandler : IUriTemplateHandler
    {

        public Uri GetRequestUri(IUrlParameterFormatter urlParameterFormatter, RestMethodInfo restMethod, object[] paramList, string basePath = "")
        {
            return null;
        }
    }

}
