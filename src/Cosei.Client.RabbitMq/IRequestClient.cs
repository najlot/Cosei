using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cosei.Client.RabbitMq
{
	public interface IRequestClient : IDisposable
	{
		Dictionary<string, string> DefaultHeaders { get; }

		Task<Response> DeleteAsync(string requestUri);

		Task<Response> GetAsync(string requestUri);

		Task<Response> PostAsync(string requestUri, string request, string contentType);

		Task<Response> PutAsync(string requestUri, string request, string contentType);
	}
}