using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cosei.Client.Base
{
	public static class RequestClientExtension
	{
		public static async Task<TResponse> GetAsync<TResponse>(this IRequestClient client, string requestUri, Dictionary<string, string> headers = null)
		{
			var result = await client.GetAsync(requestUri, headers);
			var bytes = result.EnsureSuccessStatusCode().Body.ToArray();
			return JsonSerializer.Deserialize<TResponse>(bytes, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		}

		public static async Task<TResponse> PostAsync<TResponse, TRequest>(this IRequestClient client, string requestUri, TRequest request, Dictionary<string, string> headers = null)
		{
			var requestString = JsonSerializer.Serialize(request);
			var result = await client.PostAsync(requestUri, requestString, "application/json", headers);
			var bytes = result.EnsureSuccessStatusCode().Body.ToArray();
			return JsonSerializer.Deserialize<TResponse>(bytes, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		}

		public static async Task PostAsync<TRequest>(this IRequestClient client, string requestUri, TRequest request, Dictionary<string, string> headers = null)
		{
			var requestString = JsonSerializer.Serialize(request);
			var result = await client.PostAsync(requestUri, requestString, "application/json", headers);
			result.EnsureSuccessStatusCode();
		}

		public static async Task<TResponse> PutAsync<TResponse, TRequest>(this IRequestClient client, string requestUri, TRequest request, Dictionary<string, string> headers = null)
		{
			var requestString = JsonSerializer.Serialize(request);
			var result = await client.PutAsync(requestUri, requestString, "application/json", headers);
			var bytes = result.EnsureSuccessStatusCode().Body.ToArray();
			return JsonSerializer.Deserialize<TResponse>(bytes, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		}

		public static async Task PutAsync<TRequest>(this IRequestClient client, string requestUri, TRequest request, Dictionary<string, string> headers = null)
		{
			var requestString = JsonSerializer.Serialize(request);
			var result = await client.PutAsync(requestUri, requestString, "application/json", headers);
			result.EnsureSuccessStatusCode();
		}

		public static async Task<TResponse> DeleteAsync<TResponse>(this IRequestClient client, string requestUri, Dictionary<string, string> headers = null)
		{
			var result = await client.DeleteAsync(requestUri, headers);
			result = result.EnsureSuccessStatusCode();
			return JsonSerializer.Deserialize<TResponse>(result.Body.ToArray(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		}
	}
}
