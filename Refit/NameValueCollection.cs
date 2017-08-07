using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Specialized
{
    class NameValueCollection : Dictionary<string, string>
    {
        public string[] AllKeys { get { return Keys.ToArray(); } }

        public bool HasKeys() 
        {
            return Count > 0; 
        }
    }
}