#define NET_4_0

//
// Authors:
//   Patrik Torstensson (Patrik.Torstensson@labs2.com)
//   Wictor WilÃ©n (decode/encode functions) (wictor@ibizkit.se)
//   Tim Coleman (tim@timcoleman.com)
//   Gonzalo Paniagua Javier (gonzalo@ximian.com)

//   Marek Habersack <mhabersack@novell.com>
//
// (C) 2005-2010 Novell, Inc (http://novell.com/)
//

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections.Generic;
#if !WINDOWS_APP
using System.Configuration;
#endif
using System.IO;
using System.Linq;
using System.Text;

namespace System.Web
{
#if NET_4_0
	public
#endif
	class HttpEncoder
	{
		static char[] hexChars = "0123456789abcdef".ToCharArray();
		static object entitiesLock = new object();
		static List<KeyValuePair<string, char>> entities;
#if NET_4_0
		static Lazy <HttpEncoder> defaultEncoder;
		static Lazy <HttpEncoder> currentEncoderLazy;
#else
		static HttpEncoder defaultEncoder;
#endif
		static HttpEncoder currentEncoder;

		static List<KeyValuePair<string, char>> Entities
		{
			get
			{
				lock (entitiesLock)
				{
					if (entities == null)
						InitEntities();

					return entities;
				}
			}
		}

		public static HttpEncoder Current
		{
			get
			{
#if NET_4_0
				if (currentEncoder == null)
					currentEncoder = currentEncoderLazy.Value;
#endif
				return currentEncoder;
			}
#if NET_4_0
			set {
				if (value == null)
					throw new ArgumentNullException ("value");
				currentEncoder = value;
			}
#endif
		}

		public static HttpEncoder Default
		{
			get
			{
#if NET_4_0
				return defaultEncoder.Value;
#else
				return defaultEncoder;
#endif
			}
		}

		static HttpEncoder()
		{
#if NET_4_0
			defaultEncoder = new Lazy <HttpEncoder> (() => new HttpEncoder ());
			currentEncoderLazy = new Lazy <HttpEncoder> (new Func <HttpEncoder> (GetCustomEncoderFromConfig));
#else
			defaultEncoder = new HttpEncoder();
			currentEncoder = defaultEncoder;
#endif
		}

		public HttpEncoder()
		{
		}
#if NET_4_0	
		protected internal virtual
#else
		internal static
#endif
 void HeaderNameValueEncode(string headerName, string headerValue, out string encodedHeaderName, out string encodedHeaderValue)
		{
			if (String.IsNullOrEmpty(headerName))
				encodedHeaderName = headerName;
			else
				encodedHeaderName = EncodeHeaderString(headerName);

			if (String.IsNullOrEmpty(headerValue))
				encodedHeaderValue = headerValue;
			else
				encodedHeaderValue = EncodeHeaderString(headerValue);
		}

		static void StringBuilderAppend(string s, ref StringBuilder sb)
		{
			if (sb == null)
				sb = new StringBuilder(s);
			else
				sb.Append(s);
		}

		static string EncodeHeaderString(string input)
		{
			StringBuilder sb = null;
			char ch;

			for (int i = 0; i < input.Length; i++)
			{
				ch = input[i];

				if ((ch < 32 && ch != 9) || ch == 127)
					StringBuilderAppend(String.Format("%{0:x2}", (int)ch), ref sb);
			}

			if (sb != null)
				return sb.ToString();

			return input;
		}
#if NET_4_0		
		protected internal virtual void HtmlAttributeEncode (string value, TextWriter output)
		{

			if (output == null)
				throw new ArgumentNullException ("output");

			if (String.IsNullOrEmpty (value))
				return;

			output.Write (HtmlAttributeEncode (value));
		}

		protected internal virtual void HtmlDecode (string value, TextWriter output)
		{
			if (output == null)
				throw new ArgumentNullException ("output");

			output.Write (HtmlDecode (value));
		}

		protected internal virtual void HtmlEncode (string value, TextWriter output)
		{
			if (output == null)
				throw new ArgumentNullException ("output");

			output.Write (HtmlEncode (value));
		}

		protected internal virtual byte[] UrlEncode (byte[] bytes, int offset, int count)
		{
			return UrlEncodeToBytes (bytes, offset, count);
		}

		static HttpEncoder GetCustomEncoderFromConfig ()
		{
            return new HttpEncoder();
		}
#endif
#if NET_4_0
		protected internal virtual
#else
		internal static
#endif
        string UrlPathEncode(string value)
		{
			if (String.IsNullOrEmpty(value))
				return value;

			MemoryStream result = new MemoryStream();
			int length = value.Length;
			for (int i = 0; i < length; i++)
				UrlPathEncodeChar(value[i], result);

            var encodedBytes = result.ToArray();
			return Encoding.UTF8.GetString(encodedBytes, 0, encodedBytes.Length);
		}

		internal static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count)
		{
			if (bytes == null)
				throw new ArgumentNullException("bytes");

			int blen = bytes.Length;
			if (blen == 0)
				return new byte[0];

			if (offset < 0 || offset >= blen)
				throw new ArgumentOutOfRangeException("offset");

			if (count < 0 || count > blen - offset)
				throw new ArgumentOutOfRangeException("count");

			MemoryStream result = new MemoryStream(count);
			int end = offset + count;
			for (int i = offset; i < end; i++)
				UrlEncodeChar((char)bytes[i], result, false);

			return result.ToArray();
		}

		internal static string HtmlEncode(string s)
		{
			if (s == null)
				return null;

			if (s.Length == 0)
				return String.Empty;

			bool needEncode = false;
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				if (c == '&' || c == '"' || c == '<' || c == '>' || c > 159
#if NET_4_0
				    || c == '\''
#endif
)
				{
					needEncode = true;
					break;
				}
			}

			if (!needEncode)
				return s;

			StringBuilder output = new StringBuilder();
			char ch;
			int len = s.Length;

			for (int i = 0; i < len; i++)
			{
				switch (s[i])
				{
					case '&':
						output.Append("&amp;");
						break;
					case '>':
						output.Append("&gt;");
						break;
					case '<':
						output.Append("&lt;");
						break;
					case '"':
						output.Append("&quot;");
						break;
#if NET_4_0
					case '\'':
						output.Append ("&#39;");
						break;
#endif
					case '\uff1c':
						output.Append("&#65308;");
						break;

					case '\uff1e':
						output.Append("&#65310;");
						break;

					default:
						ch = s[i];
						if (ch > 159 && ch < 256)
						{
							output.Append("&#");
							output.Append(((int)ch).ToString(Helpers.InvariantCulture));
							output.Append(";");
						}
						else
							output.Append(ch);
						break;
				}
			}

			return output.ToString();
		}

		internal static string HtmlAttributeEncode(string s)
		{
#if NET_4_0
			if (String.IsNullOrEmpty (s))
				return String.Empty;
#else
			if (s == null)
				return null;

			if (s.Length == 0)
				return String.Empty;
#endif
			bool needEncode = false;
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				if (c == '&' || c == '"' || c == '<'
#if NET_4_0
				    || c == '\''
#endif
)
				{
					needEncode = true;
					break;
				}
			}

			if (!needEncode)
				return s;

			StringBuilder output = new StringBuilder();
			int len = s.Length;
			for (int i = 0; i < len; i++)
				switch (s[i])
				{
					case '&':
						output.Append("&amp;");
						break;
					case '"':
						output.Append("&quot;");
						break;
					case '<':
						output.Append("&lt;");
						break;
#if NET_4_0
				case '\'':
					output.Append ("&#39;");
					break;
#endif
					default:
						output.Append(s[i]);
						break;
				}

			return output.ToString();
		}

		internal static string HtmlDecode(string s)
		{
			if (s == null)
				return null;

			if (s.Length == 0)
				return String.Empty;

			if (s.IndexOf('&') == -1)
				return s;
#if NET_4_0
			StringBuilder rawEntity = new StringBuilder ();
#endif
			StringBuilder entity = new StringBuilder();
			StringBuilder output = new StringBuilder();
			int len = s.Length;
			// 0 -> nothing,
			// 1 -> right after '&'
			// 2 -> between '&' and ';' but no '#'
			// 3 -> '#' found after '&' and getting numbers
			int state = 0;
			int number = 0;
			bool is_hex_value = false;
			bool have_trailing_digits = false;

			for (int i = 0; i < len; i++)
			{
				char c = s[i];
				if (state == 0)
				{
					if (c == '&')
					{
						entity.Append(c);
#if NET_4_0
						rawEntity.Append (c);
#endif
						state = 1;
					}
					else
					{
						output.Append(c);
					}
					continue;
				}

				if (c == '&')
				{
					state = 1;
					if (have_trailing_digits)
					{
						entity.Append(number.ToString(Helpers.InvariantCulture));
						have_trailing_digits = false;
					}

					output.Append(entity.ToString());
					entity.Length = 0;
					entity.Append('&');
					continue;
				}

				if (state == 1)
				{
					if (c == ';')
					{
						state = 0;
						output.Append(entity.ToString());
						output.Append(c);
						entity.Length = 0;
					}
					else
					{
						number = 0;
						is_hex_value = false;
						if (c != '#')
						{
							state = 2;
						}
						else
						{
							state = 3;
						}
						entity.Append(c);
#if NET_4_0
						rawEntity.Append (c);
#endif
					}
				}
				else if (state == 2)
				{
					entity.Append(c);
					if (c == ';')
					{
						string key = entity.ToString();

						if (key.Length > 1)
						{
							string entityName = key.Substring(1, key.Length - 2);
							int result = Entities.BinarySearch(new KeyValuePair<string, char>(entityName, ' '), new EntityNameComparer());
							if (result >= 0)
							{
#if !WINDOWS_APP
								key = Entities[result].Value.ToString(Helpers.InvariantCulture);
#else
                                key = Entities[result].Value.ToString(); // What will this do?
#endif
							}
						}

						output.Append(key);
						state = 0;
						entity.Length = 0;
#if NET_4_0
						rawEntity.Length = 0;
#endif
					}
				}
				else if (state == 3)
				{
					if (c == ';')
					{
#if NET_4_0
						if (number == 0)
							output.Append (rawEntity.ToString () + ";");
						else
#endif
						if (number > 65535)
						{
							output.Append("&#");
							output.Append(number.ToString(Helpers.InvariantCulture));
							output.Append(";");
						}
						else
						{
							output.Append((char)number);
						}
						state = 0;
						entity.Length = 0;
#if NET_4_0
						rawEntity.Length = 0;
#endif
						have_trailing_digits = false;
					}
					else if (is_hex_value && Uri.IsHexDigit(c))
					{
						number = number * 16 + Uri.FromHex(c);
						have_trailing_digits = true;
#if NET_4_0
						rawEntity.Append (c);
#endif
					}
					else if (Char.IsDigit(c))
					{
						number = number * 10 + ((int)c - '0');
						have_trailing_digits = true;
#if NET_4_0
						rawEntity.Append (c);
#endif
					}
					else if (number == 0 && (c == 'x' || c == 'X'))
					{
						is_hex_value = true;
#if NET_4_0
						rawEntity.Append (c);
#endif
					}
					else
					{
						state = 2;
						if (have_trailing_digits)
						{
							entity.Append(number.ToString(Helpers.InvariantCulture));
							have_trailing_digits = false;
						}
						entity.Append(c);
					}
				}
			}

			if (entity.Length > 0)
			{
				output.Append(entity.ToString());
			}
			else if (have_trailing_digits)
			{
				output.Append(number.ToString(Helpers.InvariantCulture));
			}
			return output.ToString();
		}

		internal static bool NotEncoded(char c)
		{
			return (c == '!' || c == '(' || c == ')' || c == '*' || c == '-' || c == '.' || c == '_'
#if !NET_4_0
 || c == '\''
#endif
);
		}

		internal static void UrlEncodeChar(char c, Stream result, bool isUnicode)
		{
			if (c > 255)
			{
				//FIXME: what happens when there is an internal error?
				//if (!isUnicode)
				//	throw new ArgumentOutOfRangeException ("c", c, "c must be less than 256");
				int idx;
				int i = (int)c;

				result.WriteByte((byte)'%');
				result.WriteByte((byte)'u');
				idx = i >> 12;
				result.WriteByte((byte)hexChars[idx]);
				idx = (i >> 8) & 0x0F;
				result.WriteByte((byte)hexChars[idx]);
				idx = (i >> 4) & 0x0F;
				result.WriteByte((byte)hexChars[idx]);
				idx = i & 0x0F;
				result.WriteByte((byte)hexChars[idx]);
				return;
			}

			if (c > ' ' && NotEncoded(c))
			{
				result.WriteByte((byte)c);
				return;
			}
			if (c == ' ')
			{
				result.WriteByte((byte)'+');
				return;
			}
			if ((c < '0') ||
				(c < 'A' && c > '9') ||
				(c > 'Z' && c < 'a') ||
				(c > 'z'))
			{
				if (isUnicode && c > 127)
				{
					result.WriteByte((byte)'%');
					result.WriteByte((byte)'u');
					result.WriteByte((byte)'0');
					result.WriteByte((byte)'0');
				}
				else
					result.WriteByte((byte)'%');

				int idx = ((int)c) >> 4;
				result.WriteByte((byte)hexChars[idx]);
				idx = ((int)c) & 0x0F;
				result.WriteByte((byte)hexChars[idx]);
			}
			else
				result.WriteByte((byte)c);
		}

		internal static void UrlPathEncodeChar(char c, Stream result)
		{
			if (c < 33 || c > 126)
			{
				byte[] bIn = Encoding.UTF8.GetBytes(c.ToString());
				for (int i = 0; i < bIn.Length; i++)
				{
					result.WriteByte((byte)'%');
					int idx = ((int)bIn[i]) >> 4;
					result.WriteByte((byte)hexChars[idx]);
					idx = ((int)bIn[i]) & 0x0F;
					result.WriteByte((byte)hexChars[idx]);
				}
			}
			else if (c == ' ')
			{
				result.WriteByte((byte)'%');
				result.WriteByte((byte)'2');
				result.WriteByte((byte)'0');
			}
			else
				result.WriteByte((byte)c);
		}

		static void InitEntities()
		{
			// Build the hash table of HTML entity references.  This list comes
			// from the HTML 4.01 W3C recommendation.
			entities = new List<KeyValuePair<string, char>>
			{
				new KeyValuePair<string, char>("nbsp", '\u00A0'),
				new KeyValuePair<string, char>("iexcl", '\u00A1'),
				new KeyValuePair<string, char>("cent", '\u00A2'),
				new KeyValuePair<string, char>("pound", '\u00A3'),
				new KeyValuePair<string, char>("curren", '\u00A4'),
				new KeyValuePair<string, char>("yen", '\u00A5'),
				new KeyValuePair<string, char>("brvbar", '\u00A6'),
				new KeyValuePair<string, char>("sect", '\u00A7'),
				new KeyValuePair<string, char>("uml", '\u00A8'),
				new KeyValuePair<string, char>("copy", '\u00A9'),
				new KeyValuePair<string, char>("ordf", '\u00AA'),
				new KeyValuePair<string, char>("laquo", '\u00AB'),
				new KeyValuePair<string, char>("not", '\u00AC'),
				new KeyValuePair<string, char>("shy", '\u00AD'),
				new KeyValuePair<string, char>("reg", '\u00AE'),
				new KeyValuePair<string, char>("macr", '\u00AF'),
				new KeyValuePair<string, char>("deg", '\u00B0'),
				new KeyValuePair<string, char>("plusmn", '\u00B1'),
				new KeyValuePair<string, char>("sup2", '\u00B2'),
				new KeyValuePair<string, char>("sup3", '\u00B3'),
				new KeyValuePair<string, char>("acute", '\u00B4'),
				new KeyValuePair<string, char>("micro", '\u00B5'),
				new KeyValuePair<string, char>("para", '\u00B6'),
				new KeyValuePair<string, char>("middot", '\u00B7'),
				new KeyValuePair<string, char>("cedil", '\u00B8'),
				new KeyValuePair<string, char>("sup1", '\u00B9'),
				new KeyValuePair<string, char>("ordm", '\u00BA'),
				new KeyValuePair<string, char>("raquo", '\u00BB'),
				new KeyValuePair<string, char>("frac14", '\u00BC'),
				new KeyValuePair<string, char>("frac12", '\u00BD'),
				new KeyValuePair<string, char>("frac34", '\u00BE'),
				new KeyValuePair<string, char>("iquest", '\u00BF'),
				new KeyValuePair<string, char>("Agrave", '\u00C0'),
				new KeyValuePair<string, char>("Aacute", '\u00C1'),
				new KeyValuePair<string, char>("Acirc", '\u00C2'),
				new KeyValuePair<string, char>("Atilde", '\u00C3'),
				new KeyValuePair<string, char>("Auml", '\u00C4'),
				new KeyValuePair<string, char>("Aring", '\u00C5'),
				new KeyValuePair<string, char>("AElig", '\u00C6'),
				new KeyValuePair<string, char>("Ccedil", '\u00C7'),
				new KeyValuePair<string, char>("Egrave", '\u00C8'),
				new KeyValuePair<string, char>("Eacute", '\u00C9'),
				new KeyValuePair<string, char>("Ecirc", '\u00CA'),
				new KeyValuePair<string, char>("Euml", '\u00CB'),
				new KeyValuePair<string, char>("Igrave", '\u00CC'),
				new KeyValuePair<string, char>("Iacute", '\u00CD'),
				new KeyValuePair<string, char>("Icirc", '\u00CE'),
				new KeyValuePair<string, char>("Iuml", '\u00CF'),
				new KeyValuePair<string, char>("ETH", '\u00D0'),
				new KeyValuePair<string, char>("Ntilde", '\u00D1'),
				new KeyValuePair<string, char>("Ograve", '\u00D2'),
				new KeyValuePair<string, char>("Oacute", '\u00D3'),
				new KeyValuePair<string, char>("Ocirc", '\u00D4'),
				new KeyValuePair<string, char>("Otilde", '\u00D5'),
				new KeyValuePair<string, char>("Ouml", '\u00D6'),
				new KeyValuePair<string, char>("times", '\u00D7'),
				new KeyValuePair<string, char>("Oslash", '\u00D8'),
				new KeyValuePair<string, char>("Ugrave", '\u00D9'),
				new KeyValuePair<string, char>("Uacute", '\u00DA'),
				new KeyValuePair<string, char>("Ucirc", '\u00DB'),
				new KeyValuePair<string, char>("Uuml", '\u00DC'),
				new KeyValuePair<string, char>("Yacute", '\u00DD'),
				new KeyValuePair<string, char>("THORN", '\u00DE'),
				new KeyValuePair<string, char>("szlig", '\u00DF'),
				new KeyValuePair<string, char>("agrave", '\u00E0'),
				new KeyValuePair<string, char>("aacute", '\u00E1'),
				new KeyValuePair<string, char>("acirc", '\u00E2'),
				new KeyValuePair<string, char>("atilde", '\u00E3'),
				new KeyValuePair<string, char>("auml", '\u00E4'),
				new KeyValuePair<string, char>("aring", '\u00E5'),
				new KeyValuePair<string, char>("aelig", '\u00E6'),
				new KeyValuePair<string, char>("ccedil", '\u00E7'),
				new KeyValuePair<string, char>("egrave", '\u00E8'),
				new KeyValuePair<string, char>("eacute", '\u00E9'),
				new KeyValuePair<string, char>("ecirc", '\u00EA'),
				new KeyValuePair<string, char>("euml", '\u00EB'),
				new KeyValuePair<string, char>("igrave", '\u00EC'),
				new KeyValuePair<string, char>("iacute", '\u00ED'),
				new KeyValuePair<string, char>("icirc", '\u00EE'),
				new KeyValuePair<string, char>("iuml", '\u00EF'),
				new KeyValuePair<string, char>("eth", '\u00F0'),
				new KeyValuePair<string, char>("ntilde", '\u00F1'),
				new KeyValuePair<string, char>("ograve", '\u00F2'),
				new KeyValuePair<string, char>("oacute", '\u00F3'),
				new KeyValuePair<string, char>("ocirc", '\u00F4'),
				new KeyValuePair<string, char>("otilde", '\u00F5'),
				new KeyValuePair<string, char>("ouml", '\u00F6'),
				new KeyValuePair<string, char>("divide", '\u00F7'),
				new KeyValuePair<string, char>("oslash", '\u00F8'),
				new KeyValuePair<string, char>("ugrave", '\u00F9'),
				new KeyValuePair<string, char>("uacute", '\u00FA'),
				new KeyValuePair<string, char>("ucirc", '\u00FB'),
				new KeyValuePair<string, char>("uuml", '\u00FC'),
				new KeyValuePair<string, char>("yacute", '\u00FD'),
				new KeyValuePair<string, char>("thorn", '\u00FE'),
				new KeyValuePair<string, char>("yuml", '\u00FF'),
				new KeyValuePair<string, char>("fnof", '\u0192'),
				new KeyValuePair<string, char>("Alpha", '\u0391'),
				new KeyValuePair<string, char>("Beta", '\u0392'),
				new KeyValuePair<string, char>("Gamma", '\u0393'),
				new KeyValuePair<string, char>("Delta", '\u0394'),
				new KeyValuePair<string, char>("Epsilon", '\u0395'),
				new KeyValuePair<string, char>("Zeta", '\u0396'),
				new KeyValuePair<string, char>("Eta", '\u0397'),
				new KeyValuePair<string, char>("Theta", '\u0398'),
				new KeyValuePair<string, char>("Iota", '\u0399'),
				new KeyValuePair<string, char>("Kappa", '\u039A'),
				new KeyValuePair<string, char>("Lambda", '\u039B'),
				new KeyValuePair<string, char>("Mu", '\u039C'),
				new KeyValuePair<string, char>("Nu", '\u039D'),
				new KeyValuePair<string, char>("Xi", '\u039E'),
				new KeyValuePair<string, char>("Omicron", '\u039F'),
				new KeyValuePair<string, char>("Pi", '\u03A0'),
				new KeyValuePair<string, char>("Rho", '\u03A1'),
				new KeyValuePair<string, char>("Sigma", '\u03A3'),
				new KeyValuePair<string, char>("Tau", '\u03A4'),
				new KeyValuePair<string, char>("Upsilon", '\u03A5'),
				new KeyValuePair<string, char>("Phi", '\u03A6'),
				new KeyValuePair<string, char>("Chi", '\u03A7'),
				new KeyValuePair<string, char>("Psi", '\u03A8'),
				new KeyValuePair<string, char>("Omega", '\u03A9'),
				new KeyValuePair<string, char>("alpha", '\u03B1'),
				new KeyValuePair<string, char>("beta", '\u03B2'),
				new KeyValuePair<string, char>("gamma", '\u03B3'),
				new KeyValuePair<string, char>("delta", '\u03B4'),
				new KeyValuePair<string, char>("epsilon", '\u03B5'),
				new KeyValuePair<string, char>("zeta", '\u03B6'),
				new KeyValuePair<string, char>("eta", '\u03B7'),
				new KeyValuePair<string, char>("theta", '\u03B8'),
				new KeyValuePair<string, char>("iota", '\u03B9'),
				new KeyValuePair<string, char>("kappa", '\u03BA'),
				new KeyValuePair<string, char>("lambda", '\u03BB'),
				new KeyValuePair<string, char>("mu", '\u03BC'),
				new KeyValuePair<string, char>("nu", '\u03BD'),
				new KeyValuePair<string, char>("xi", '\u03BE'),
				new KeyValuePair<string, char>("omicron", '\u03BF'),
				new KeyValuePair<string, char>("pi", '\u03C0'),
				new KeyValuePair<string, char>("rho", '\u03C1'),
				new KeyValuePair<string, char>("sigmaf", '\u03C2'),
				new KeyValuePair<string, char>("sigma", '\u03C3'),
				new KeyValuePair<string, char>("tau", '\u03C4'),
				new KeyValuePair<string, char>("upsilon", '\u03C5'),
				new KeyValuePair<string, char>("phi", '\u03C6'),
				new KeyValuePair<string, char>("chi", '\u03C7'),
				new KeyValuePair<string, char>("psi", '\u03C8'),
				new KeyValuePair<string, char>("omega", '\u03C9'),
				new KeyValuePair<string, char>("thetasym", '\u03D1'),
				new KeyValuePair<string, char>("upsih", '\u03D2'),
				new KeyValuePair<string, char>("piv", '\u03D6'),
				new KeyValuePair<string, char>("bull", '\u2022'),
				new KeyValuePair<string, char>("hellip", '\u2026'),
				new KeyValuePair<string, char>("prime", '\u2032'),
				new KeyValuePair<string, char>("Prime", '\u2033'),
				new KeyValuePair<string, char>("oline", '\u203E'),
				new KeyValuePair<string, char>("frasl", '\u2044'),
				new KeyValuePair<string, char>("weierp", '\u2118'),
				new KeyValuePair<string, char>("image", '\u2111'),
				new KeyValuePair<string, char>("real", '\u211C'),
				new KeyValuePair<string, char>("trade", '\u2122'),
				new KeyValuePair<string, char>("alefsym", '\u2135'),
				new KeyValuePair<string, char>("larr", '\u2190'),
				new KeyValuePair<string, char>("uarr", '\u2191'),
				new KeyValuePair<string, char>("rarr", '\u2192'),
				new KeyValuePair<string, char>("darr", '\u2193'),
				new KeyValuePair<string, char>("harr", '\u2194'),
				new KeyValuePair<string, char>("crarr", '\u21B5'),
				new KeyValuePair<string, char>("lArr", '\u21D0'),
				new KeyValuePair<string, char>("uArr", '\u21D1'),
				new KeyValuePair<string, char>("rArr", '\u21D2'),
				new KeyValuePair<string, char>("dArr", '\u21D3'),
				new KeyValuePair<string, char>("hArr", '\u21D4'),
				new KeyValuePair<string, char>("forall", '\u2200'),
				new KeyValuePair<string, char>("part", '\u2202'),
				new KeyValuePair<string, char>("exist", '\u2203'),
				new KeyValuePair<string, char>("empty", '\u2205'),
				new KeyValuePair<string, char>("nabla", '\u2207'),
				new KeyValuePair<string, char>("isin", '\u2208'),
				new KeyValuePair<string, char>("notin", '\u2209'),
				new KeyValuePair<string, char>("ni", '\u220B'),
				new KeyValuePair<string, char>("prod", '\u220F'),
				new KeyValuePair<string, char>("sum", '\u2211'),
				new KeyValuePair<string, char>("minus", '\u2212'),
				new KeyValuePair<string, char>("lowast", '\u2217'),
				new KeyValuePair<string, char>("radic", '\u221A'),
				new KeyValuePair<string, char>("prop", '\u221D'),
				new KeyValuePair<string, char>("infin", '\u221E'),
				new KeyValuePair<string, char>("ang", '\u2220'),
				new KeyValuePair<string, char>("and", '\u2227'),
				new KeyValuePair<string, char>("or", '\u2228'),
				new KeyValuePair<string, char>("cap", '\u2229'),
				new KeyValuePair<string, char>("cup", '\u222A'),
				new KeyValuePair<string, char>("int", '\u222B'),
				new KeyValuePair<string, char>("there4", '\u2234'),
				new KeyValuePair<string, char>("sim", '\u223C'),
				new KeyValuePair<string, char>("cong", '\u2245'),
				new KeyValuePair<string, char>("asymp", '\u2248'),
				new KeyValuePair<string, char>("ne", '\u2260'),
				new KeyValuePair<string, char>("equiv", '\u2261'),
				new KeyValuePair<string, char>("le", '\u2264'),
				new KeyValuePair<string, char>("ge", '\u2265'),
				new KeyValuePair<string, char>("sub", '\u2282'),
				new KeyValuePair<string, char>("sup", '\u2283'),
				new KeyValuePair<string, char>("nsub", '\u2284'),
				new KeyValuePair<string, char>("sube", '\u2286'),
				new KeyValuePair<string, char>("supe", '\u2287'),
				new KeyValuePair<string, char>("oplus", '\u2295'),
				new KeyValuePair<string, char>("otimes", '\u2297'),
				new KeyValuePair<string, char>("perp", '\u22A5'),
				new KeyValuePair<string, char>("sdot", '\u22C5'),
				new KeyValuePair<string, char>("lceil", '\u2308'),
				new KeyValuePair<string, char>("rceil", '\u2309'),
				new KeyValuePair<string, char>("lfloor", '\u230A'),
				new KeyValuePair<string, char>("rfloor", '\u230B'),
				new KeyValuePair<string, char>("lang", '\u2329'),
				new KeyValuePair<string, char>("rang", '\u232A'),
				new KeyValuePair<string, char>("loz", '\u25CA'),
				new KeyValuePair<string, char>("spades", '\u2660'),
				new KeyValuePair<string, char>("clubs", '\u2663'),
				new KeyValuePair<string, char>("hearts", '\u2665'),
				new KeyValuePair<string, char>("diams", '\u2666'),
				new KeyValuePair<string, char>("quot", '\u0022'),
				new KeyValuePair<string, char>("amp", '\u0026'),
				new KeyValuePair<string, char>("lt", '\u003C'),
				new KeyValuePair<string, char>("gt", '\u003E'),
				new KeyValuePair<string, char>("OElig", '\u0152'),
				new KeyValuePair<string, char>("oelig", '\u0153'),
				new KeyValuePair<string, char>("Scaron", '\u0160'),
				new KeyValuePair<string, char>("scaron", '\u0161'),
				new KeyValuePair<string, char>("Yuml", '\u0178'),
				new KeyValuePair<string, char>("circ", '\u02C6'),
				new KeyValuePair<string, char>("tilde", '\u02DC'),
				new KeyValuePair<string, char>("ensp", '\u2002'),
				new KeyValuePair<string, char>("emsp", '\u2003'),
				new KeyValuePair<string, char>("thinsp", '\u2009'),
				new KeyValuePair<string, char>("zwnj", '\u200C'),
				new KeyValuePair<string, char>("zwj", '\u200D'),
				new KeyValuePair<string, char>("lrm", '\u200E'),
				new KeyValuePair<string, char>("rlm", '\u200F'),
				new KeyValuePair<string, char>("ndash", '\u2013'),
				new KeyValuePair<string, char>("mdash", '\u2014'),
				new KeyValuePair<string, char>("lsquo", '\u2018'),
				new KeyValuePair<string, char>("rsquo", '\u2019'),
				new KeyValuePair<string, char>("sbquo", '\u201A'),
				new KeyValuePair<string, char>("ldquo", '\u201C'),
				new KeyValuePair<string, char>("rdquo", '\u201D'),
				new KeyValuePair<string, char>("bdquo", '\u201E'),
				new KeyValuePair<string, char>("dagger", '\u2020'),
				new KeyValuePair<string, char>("Dagger", '\u2021'),
				new KeyValuePair<string, char>("permil", '\u2030'),
				new KeyValuePair<string, char>("lsaquo", '\u2039'),
				new KeyValuePair<string, char>("rsaquo", '\u203A'),
				new KeyValuePair<string, char>("euro", '\u20AC')
			};

		    entities = entities.OrderBy(x=>x.Key).ToList();
		}
	}

	class EntityNameComparer: IComparer<KeyValuePair<string, char>>
	{
		public int Compare(KeyValuePair<string, char> x, KeyValuePair<string, char> y)
		{
			return String.Compare(x.Key, y.Key, StringComparison.Ordinal);
		}
	}

#if WINDOWS_APP
    // Fine, I'll just make my own
    static class Uri
    {
        static readonly Dictionary<char, int> hexDigits;

        static Uri()
        {
            hexDigits = new[] {
                '0', '1', '2', '3', '4', '5', '6', '7',
                '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'
            }.Zip(Enumerable.Range(0, 0xf), (c, i) => new { c, i })
             .ToDictionary(k => k.c, v => v.i);
        }

        public static bool IsHexDigit(char c)
        {
            return hexDigits.ContainsKey(Char.ToLowerInvariant(c));
        }

        public static int FromHex(char digit)
        {
            if(!IsHexDigit(digit))
                throw new ArgumentOutOfRangeException("digit");

            return hexDigits[digit];
        }
  }
#endif
}
