using Cosei.Client.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cosei.Client.Http
{
    public class HttpRequestClient : IRequestClient
	{
		private HttpClient _client;
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

		public HttpRequestClient(string baseUrl)
		{
			_client = new HttpClient
			{
				BaseAddress = new Uri(baseUrl)
			};
		}

		public async Task<Response> GetAsync(string requestUri, Dictionary<string, string> headers = null)
		{
			await _semaphore.WaitAsync();

			try
			{
				_client.DefaultRequestHeaders.Clear();

				if (headers != null)
				{
					foreach (var header in headers)
					{
						_client.DefaultRequestHeaders.Add(header.Key, header.Value);
					}
				}

				var response = await _client.GetAsync(requestUri);
				var body = await response.Content.ReadAsByteArrayAsync();

				var contentType = "text/plain";

				if (response.Headers.TryGetValues("Content-Type", out var type))
				{
					contentType = type.FirstOrDefault() ?? contentType;
				}

				return new Response((int)response.StatusCode, contentType, body);
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public async Task<Response> PostAsync(string requestUri, string request, string contentType, Dictionary<string, string> headers = null)
		{
			await _semaphore.WaitAsync();

			try
			{
				_client.DefaultRequestHeaders.Clear();

				if (headers != null)
				{
					foreach (var header in headers)
					{
						_client.DefaultRequestHeaders.Add(header.Key, header.Value);
					}
				}

				var content = new StringContent(request, Encoding.UTF8, contentType);
				var response = await _client.PostAsync(requestUri, content);
				var body = await response.Content.ReadAsByteArrayAsync();

				var responseContentType = "text/plain";

				if (response.Headers.TryGetValues("Content-Type", out var type))
				{
					responseContentType = type.FirstOrDefault() ?? responseContentType;
				}

				return new Response((int)response.StatusCode, responseContentType, body);
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public async Task<Response> PutAsync(string requestUri, string request, string contentType, Dictionary<string, string> headers = null)
		{
			await _semaphore.WaitAsync();

			try
			{
				_client.DefaultRequestHeaders.Clear();

				if (headers != null)
				{
					foreach (var header in headers)
					{
						_client.DefaultRequestHeaders.Add(header.Key, header.Value);
					}
				}

				var content = new StringContent(request, Encoding.UTF8, contentType);
				var response = await _client.PutAsync(requestUri, content);
				var body = await response.Content.ReadAsByteArrayAsync();

				var responseContentType = "text/plain";

				if (response.Headers.TryGetValues("Content-Type", out var type))
				{
					responseContentType = type.FirstOrDefault() ?? responseContentType;
				}

				return new Response((int)response.StatusCode, responseContentType, body);
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public async Task<Response> DeleteAsync(string requestUri, Dictionary<string, string> headers = null)
		{
			await _semaphore.WaitAsync();

			try
			{
				_client.DefaultRequestHeaders.Clear();

				if (headers != null)
				{
					foreach (var header in headers)
					{
						_client.DefaultRequestHeaders.Add(header.Key, header.Value);
					}
				}

				var response = await _client.DeleteAsync(requestUri);
				var body = await response.Content.ReadAsByteArrayAsync();

				var contentType = "text/plain";

				if (response.Headers.TryGetValues("Content-Type", out var type))
				{
					contentType = type.FirstOrDefault() ?? contentType;
				}

				return new Response((int)response.StatusCode, contentType, body);
			}
			finally
			{
				_semaphore.Release();
			}
		}

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				disposedValue = true;

				if (disposing)
				{
					_client?.Dispose();
					_client = null;
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion IDisposable Support
	}
}