using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using HttpUtility = System.Web.HttpUtility;
namespace Refit
{
    public class RefitSettings
    {
        public RefitSettings()
        {
            UrlParameterFormatter = new DefaultUrlParameterFormatter();
            UrlTemplateHandler = new DefaultUrlTemplateHandler();
        }
        public JsonSerializerSettings JsonSerializerSettings { get; set; }
        public IUrlParameterFormatter UrlParameterFormatter { get; set; }
        public IUrlTemplateHandler UrlTemplateHandler { get; set; }
    }

    public interface IUrlTemplateHandler
    {
        IEnumerable<string> GetParameterNames(string template);
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

    public class DefaultUrlTemplateHandler : IUrlTemplateHandler
    {
        static readonly Regex parameterRegex = new Regex(@"{(.*?)}");

        public IEnumerable<string> GetParameterNames(string template)
        {
            var parameterizedParts = template.Split('/', '?')
                .SelectMany(x => parameterRegex.Matches(x).Cast<Match>())
                .ToList();
            return parameterizedParts.Select(p => p.Groups[1].Value.ToLowerInvariant()).ToList();
        }
        public Uri GetRequestUri(IUrlParameterFormatter urlParameterFormatter, RestMethodInfo restMethod, object[] paramList, string basePath = "")
        {
            string urlTarget = (basePath == "/" ? String.Empty : basePath) + restMethod.RelativePath;
            var queryParamsToAdd = new Dictionary<string, string>();

            for (int i = 0; i < paramList.Length; i++)
            {

                if (restMethod.ParameterMap.ContainsKey(i))
                {
                    urlTarget = Regex.Replace(
                        urlTarget,
                        "{" + restMethod.ParameterMap[i] + "}",
                        urlParameterFormatter.Format(paramList[i], restMethod.ParameterInfoMap[i]),
                        RegexOptions.IgnoreCase);
                    continue;
                }

                if (restMethod.BodyParameterInfo != null && restMethod.BodyParameterInfo.Item2 == i)
                {
                    continue;
                }

                if (!restMethod.HeaderParameterMap.ContainsKey(i))
                {
                    if (paramList[i] != null)
                    {
                        queryParamsToAdd[restMethod.QueryParameterMap[i]] = urlParameterFormatter.Format(paramList[i], restMethod.ParameterInfoMap[i]);
                    }
                }
            }

            // NB: The URI methods in .NET are dumb. Also, we do this 
            // UriBuilder business so that we preserve any hardcoded query 
            // parameters as well as add the parameterized ones.
            var uri = new UriBuilder(new Uri(new Uri("http://api"), urlTarget));
            var query = HttpUtility.ParseQueryString(uri.Query ?? "");
            foreach (var kvp in queryParamsToAdd)
            {
                query.Add(kvp.Key, kvp.Value);
            }

            if (query.HasKeys())
            {
                var pairs = query.Keys.Cast<string>().Select(x => HttpUtility.UrlEncode(x) + "=" + HttpUtility.UrlEncode(query[x]));
                uri.Query = String.Join("&", pairs);
            }
            else
            {
                uri.Query = null;
            }
            return new Uri(uri.Uri.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped), UriKind.Relative);
        }

   
    }

}
