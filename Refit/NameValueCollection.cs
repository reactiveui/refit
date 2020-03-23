using System.Collections.Generic;
using System.Linq;

namespace System.Collections.Specialized
{
    class NameValueCollection : Dictionary<string, string>
    {
        public string[] AllKeys => Keys.ToArray();

    }
}
