using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Refit
{
    /// <summary>
    /// Transforms a form source from a .NET representation to the appropriate HTTP form encoded representation.
    /// </summary>
    /// <remarks>Performs field renaming and value formatting as specified in <see cref="QueryAttribute"/>s and
    /// <see cref="RefitSettings.FormUrlEncodedParameterFormatter"/>. A given key may appear multiple times with the
    /// same or different values.</remarks>
    class FormValueMultimap : IEnumerable<KeyValuePair<string?, string?>>
    {
        static readonly Dictionary<Type, PropertyInfo[]> PropertyCache = new();

        readonly IList<KeyValuePair<string?, string?>> formEntries = new List<KeyValuePair<string?, string?>>();

        readonly IHttpContentSerializer contentSerializer;

        public FormValueMultimap(object source, RefitSettings settings)
        {
            if (settings is null)
                throw new ArgumentNullException(nameof(settings));
            contentSerializer = settings.ContentSerializer;

            if (source == null) return;

            if (source is IDictionary dictionary)
            {
                foreach (var key in dictionary.Keys)
                {
                    var value = dictionary[key];
                    if (value != null)
                    {
                        Add(key.ToString(), settings.FormUrlEncodedParameterFormatter.Format(value, null));
                    }
                }

                return;
            }

            var type = source.GetType();

            lock (PropertyCache)
            {
                if (!PropertyCache.ContainsKey(type))
                {
                    PropertyCache[type] = GetProperties(type);
                }

                foreach (var property in PropertyCache[type])
                {
                    var value = property.GetValue(source, null);
                    if (value != null)
                    {
                        var fieldName = GetFieldNameForProperty(property);

                        // see if there's a query attribute
                        var attrib = property.GetCustomAttribute<QueryAttribute>(true);

                        if (value is IEnumerable enumerable)
                        {
                            var collectionFormat = attrib != null && attrib.IsCollectionFormatSpecified
                                ? attrib.CollectionFormat
                                : settings.CollectionFormat;

                            switch (collectionFormat)
                            {
                                case CollectionFormat.Multi:
                                    foreach (var item in enumerable)
                                    {
                                        Add(fieldName, settings.FormUrlEncodedParameterFormatter.Format(item, attrib?.Format));
                                    }
                                    break;
                                case CollectionFormat.Csv:
                                case CollectionFormat.Ssv:
                                case CollectionFormat.Tsv:
                                case CollectionFormat.Pipes:
                                    var delimiter = collectionFormat switch
                                    {
                                        CollectionFormat.Csv => ",",
                                        CollectionFormat.Ssv => " ",
                                        CollectionFormat.Tsv => "\t",
                                        _ => "|"
                                    };

                                    var formattedValues = enumerable
                                        .Cast<object>()
                                        .Select(v => settings.FormUrlEncodedParameterFormatter.Format(v, attrib?.Format));
                                    Add(fieldName, string.Join(delimiter, formattedValues));
                                    break;
                                default:
                                    Add(fieldName, settings.FormUrlEncodedParameterFormatter.Format(value, attrib?.Format));
                                    break;
                            }
                        }
                        else
                        {
                            Add(fieldName, settings.FormUrlEncodedParameterFormatter.Format(value, attrib?.Format));
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Returns a key for each entry. If multiple entries share the same key, the key is returned multiple times.
        /// </summary>
        public IEnumerable<string?> Keys => this.Select(it => it.Key);

        void Add(string? key, string? value)
        {
            formEntries.Add(new KeyValuePair<string?, string?>(key, value));
        }

        string GetFieldNameForProperty(PropertyInfo propertyInfo)
        {
            var name = propertyInfo.GetCustomAttributes<AliasAsAttribute>(true)
                               .Select(a => a.Name)
                               .FirstOrDefault()
                   ?? contentSerializer.GetFieldNameForProperty(propertyInfo)
                   ?? propertyInfo.Name;

            var qattrib = propertyInfo.GetCustomAttributes<QueryAttribute>(true)
                           .Select(attr => !string.IsNullOrWhiteSpace(attr.Prefix) ? $"{attr.Prefix}{attr.Delimiter}{name}" : name)
                           .FirstOrDefault();

            return qattrib ?? name;
        }

        static PropertyInfo[] GetProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                       .Where(p => p.CanRead && p.GetMethod?.IsPublic == true)
                       .ToArray();
        }

        public IEnumerator<KeyValuePair<string?, string?>> GetEnumerator()
        {
            return formEntries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
