using System.Collections.Generic;
using System.Text;
using System;
using System.Globalization;
using Newtonsoft.Json.Serialization;

namespace Refit.Tests
{
    public class DeliminatorSeparatedPropertyNamesContractResolver : DefaultContractResolver
    {
        readonly string separator;

        protected DeliminatorSeparatedPropertyNamesContractResolver(char separator)
        {
            this.separator = separator.ToString(CultureInfo.InvariantCulture);
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            var parts = new List<string>();
            var currentWord = new StringBuilder();

            foreach (var c in propertyName.ToCharArray())
            {
                if (Char.IsUpper(c) && currentWord.Length > 0)
                {
                    parts.Add(currentWord.ToString());
                    currentWord.Clear();
                }

                currentWord.Append(char.ToLower(c));
            }

            if (currentWord.Length > 0)
            {
                parts.Add(currentWord.ToString());
            }

            return String.Join(separator, parts.ToArray());
        }
    }

    public class SnakeCasePropertyNamesContractResolver : DeliminatorSeparatedPropertyNamesContractResolver
    {
        public SnakeCasePropertyNamesContractResolver() : base('_') { }
    }
}
