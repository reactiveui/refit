using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Refit
{
    public class UnsuccessfulResponseFilterContext
    {
        /// <summary>
        /// Sets the response of the method. The return type must be compatible with the method return type.
        /// </summary>
        public Action<object> SetMethodResponse { get; internal set; }

        /// <summary>
        /// The actual http response message received from the API.
        /// </summary>
        public HttpResponseMessage HttpResponse { get; internal set; }
    }
}
