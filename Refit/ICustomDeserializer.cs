using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Refit
{
	public interface ICustomDeserializer<T>
	{
		Task<T> Deserialize(HttpResponseMessage message);
	}
}
