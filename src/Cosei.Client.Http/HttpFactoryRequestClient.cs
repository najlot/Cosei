using Cosei.Client.Base;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Cosei.Client.Http;

public class HttpFactoryRequestClient(IHttpClientFactory httpClientFactory) : IRequestClient
{
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

	public async Task<Response> GetAsync(string requestUri, Dictionary<string, string> headers = null)
	{
		using var client = _httpClientFactory.CreateClient();
		using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

		AddHeaders(request, headers);

		using var response = await client.SendAsync(request).ConfigureAwait(false);
		return await CreateResponseAsync(response).ConfigureAwait(false);
	}

	public async Task<Response> PostAsync(string requestUri, string request, string contentType, Dictionary<string, string> headers = null)
	{
		using var client = _httpClientFactory.CreateClient();
		using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
		{
			Content = new StringContent(request, Encoding.UTF8, contentType)
		};

		AddHeaders(httpRequest, headers);

		using var response = await client.SendAsync(httpRequest).ConfigureAwait(false);
		return await CreateResponseAsync(response).ConfigureAwait(false);
	}

	public async Task<Response> PutAsync(string requestUri, string request, string contentType, Dictionary<string, string> headers = null)
	{
		using var client = _httpClientFactory.CreateClient();
		using var httpRequest = new HttpRequestMessage(HttpMethod.Put, requestUri)
		{
			Content = new StringContent(request, Encoding.UTF8, contentType)
		};

		AddHeaders(httpRequest, headers);

		using var response = await client.SendAsync(httpRequest).ConfigureAwait(false);
		return await CreateResponseAsync(response).ConfigureAwait(false);
	}

	public async Task<Response> DeleteAsync(string requestUri, Dictionary<string, string> headers = null)
	{
		using var client = _httpClientFactory.CreateClient();
		using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);

		AddHeaders(request, headers);

		using var response = await client.SendAsync(request).ConfigureAwait(false);
		return await CreateResponseAsync(response).ConfigureAwait(false);
	}

	private static void AddHeaders(HttpRequestMessage request, Dictionary<string, string> headers)
	{
		if (headers != null)
		{
			foreach (var header in headers)
			{
				request.Headers.Add(header.Key, header.Value);
			}
		}
	}

	private static async Task<Response> CreateResponseAsync(HttpResponseMessage response)
	{
		response.EnsureSuccessStatusCode();

		var array = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

		var responseContentType = response.Content.Headers.ContentType?.ToString() ?? "text/plain";

		return new Response((int)response.StatusCode, responseContentType, array);
	}

	public void Dispose()
	{
		// Nothing to dispose
	}
}