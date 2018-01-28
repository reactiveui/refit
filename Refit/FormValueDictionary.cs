using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Refit
{
    class FormValueDictionary : Dictionary<string, string>
    {
        static readonly Dictionary<Type, PropertyInfo[]> propertyCache
            = new Dictionary<Type, PropertyInfo[]>();

        public FormValueDictionary(object source, RefitSettings settings)
        {
            if (source == null) return;

            if (source is IDictionary dictionary)
            {
                foreach (var key in dictionary.Keys)
                {
                    var value = dictionary[key];
                    if (value != null && key != null)
                    {
                        Add(key.ToString(),  settings.UrlParameterFormatter.Format(value, null));
                    }
                }

                return;
            }

            var type = source.GetType();

            lock (propertyCache)
            {
                if (!propertyCache.ContainsKey(type))
                {
                    propertyCache[type] = GetProperties(type);
                }

                foreach (var property in propertyCache[type])
                {
                    var value = property.GetValue(source, null);
                    if (value != null)
                    {
                        Add(GetFieldNameForProperty(property), settings.UrlParameterFormatter.Format(value, null));
                    }
                }
            }
        }

        string GetFieldNameForProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttributes<AliasAsAttribute>(true)
                               .Select(a => a.Name)
                               .FirstOrDefault()
                   ?? propertyInfo.GetCustomAttributes<JsonPropertyAttribute>(true)
                                  .Select(a => a.PropertyName)
                                  .FirstOrDefault()
                   ?? propertyInfo.Name;
        }

        PropertyInfo[] GetProperties(Type type)
        {
            return type.GetProperties()
                       .Where(p => p.CanRead)
                       .ToArray();
        }
    }
}
