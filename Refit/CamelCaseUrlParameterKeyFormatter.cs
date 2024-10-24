namespace Refit
{
    /// <summary>
    /// Provides an implementation of <see cref="IUrlParameterKeyFormatter"/> that formats URL parameter keys in camelCase.
    /// </summary>
    public class CamelCaseUrlParameterKeyFormatter : IUrlParameterKeyFormatter
    {
        /// <summary>
        /// Formats the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public string Format(string key)
        {
            if (string.IsNullOrEmpty(key) || !char.IsUpper(key[0]))
            {
                return key;
            }

#if NETCOREAPP
            return string.Create(
                key.Length,
                key,
                (chars, name) =>
                {
                    name.CopyTo(chars);
                    FixCasing(chars);
                }
            );
#else
            char[] chars = key.ToCharArray();
            FixCasing(chars);
            return new string(chars);
#endif
        }

        private static void FixCasing(Span<char> chars)
        {
            for (var i = 0; i < chars.Length; i++)
            {
                if (i == 1 && !char.IsUpper(chars[i]))
                {
                    break;
                }

                var hasNext = (i + 1 < chars.Length);

                // Stop when next char is already lowercase.
                if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
                {
                    break;
                }

                chars[i] = char.ToLowerInvariant(chars[i]);
            }
        }
    }
}
