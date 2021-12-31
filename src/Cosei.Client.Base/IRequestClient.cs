using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cosei.Client.Base
{
	public interface IRequestClient : IDisposable
	{
		Task<Response> GetAsync(string requestUri, Dictionary<string, string> headers = null);

		Task<Response> PostAsync(string requestUri, string request, string contentType, Dictionary<string, string> headers = null);

		Task<Response> PutAsync(string requestUri, string request, string contentType, Dictionary<string, string> headers = null);

		Task<Response> DeleteAsync(string requestUri, Dictionary<string, string> headers = null);
	}
}
