using Cosei.Client.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Cosei.Client.Http
{
    public class HttpFactoryRequestClient : IRequestClient, IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private bool _disposedValue;

        public HttpFactoryRequestClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<Response> GetAsync(string requestUri, Dictionary<string, string> headers = null)
        {
			using (var client = _httpClientFactory.CreateClient())
			{
                if (headers != null)
                {
                    foreach (KeyValuePair<string, string> header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                var response = await client.GetAsync(requestUri);
                byte[] array = await response.Content.ReadAsByteArrayAsync();
                string text = "text/plain";
                if (response.Headers.TryGetValues("Content-Type", out var values))
                {
                    text = values.FirstOrDefault() ?? text;
                }

                return new Response((int)response.StatusCode, text, array);
            }
        }

        public async Task<Response> PostAsync(string requestUri, string request, string contentType, Dictionary<string, string> headers = null)
        {
			using (var client = _httpClientFactory.CreateClient())
			{
				if (headers != null)
				{
					foreach (KeyValuePair<string, string> header in headers)
					{
						client.DefaultRequestHeaders.Add(header.Key, header.Value);
					}
				}

				var content = new StringContent(request, Encoding.UTF8, contentType);
				var response = await client.PostAsync(requestUri, content);
				byte[] array = await response.Content.ReadAsByteArrayAsync();
				string text = "text/plain";
				if (response.Headers.TryGetValues("Content-Type", out var values))
				{
					text = values.FirstOrDefault() ?? text;
				}

				return new Response((int)response.StatusCode, text, array);
            }
        }

        public async Task<Response> PutAsync(string requestUri, string request, string contentType, Dictionary<string, string> headers = null)
        {
			using (var client = _httpClientFactory.CreateClient())
			{

				if (headers != null)
				{
					foreach (KeyValuePair<string, string> header in headers)
					{
						client.DefaultRequestHeaders.Add(header.Key, header.Value);
					}
				}

				var content = new StringContent(request, Encoding.UTF8, contentType);
				var response = await client.PutAsync(requestUri, content);
				byte[] array = await response.Content.ReadAsByteArrayAsync();
				string text = "text/plain";
				if (response.Headers.TryGetValues("Content-Type", out var values))
				{
					text = values.FirstOrDefault() ?? text;
				}

				return new Response((int)response.StatusCode, text, array);
            }
        }

        public async Task<Response> DeleteAsync(string requestUri, Dictionary<string, string> headers = null)
        {
			using (var client = _httpClientFactory.CreateClient())
			{
                if (headers != null)
                {
                    foreach (KeyValuePair<string, string> header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                var response = await client.DeleteAsync(requestUri);
                byte[] array = await response.Content.ReadAsByteArrayAsync();
                string text = "text/plain";
                if (response.Headers.TryGetValues("Content-Type", out var values))
                {
                    text = values.FirstOrDefault() ?? text;
                }

                return new Response((int)response.StatusCode, text, array);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _disposedValue = true;
                if (disposing)
                {
                }
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}