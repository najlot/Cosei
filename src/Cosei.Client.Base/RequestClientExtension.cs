using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cosei.Client.Base;

public static class RequestClientExtension
{
	private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

	public static async Task<TResponse> GetAsync<TResponse>(
		this IRequestClient client,
		string requestUri,
		Dictionary<string, string> headers = null)
	{
		var result = await client
			.GetAsync(requestUri, headers)
			.ConfigureAwait(false);

		var bodyBytes = result
			.EnsureSuccessStatusCode()
			.Body
			.ToArray();

		return JsonSerializer.Deserialize<TResponse>(bodyBytes, _options);
	}

	public static async Task<TResponse> PostAsync<TResponse, TRequest>(
		this IRequestClient client,
		string requestUri,
		TRequest request,
		Dictionary<string, string> headers = null)
	{
		var requestString = JsonSerializer.Serialize(request);

		var result = await client
			.PostAsync(requestUri, requestString, "application/json", headers)
			.ConfigureAwait(false);

		var bodyBytes = result
			.EnsureSuccessStatusCode()
			.Body
			.ToArray();

		return JsonSerializer.Deserialize<TResponse>(bodyBytes, _options);
	}

	public static async Task PostAsync<TRequest>(
		this IRequestClient client,
		string requestUri,
		TRequest request,
		Dictionary<string, string> headers = null)
	{
		var requestString = JsonSerializer.Serialize(request);

		var result = await client
			.PostAsync(requestUri, requestString, "application/json", headers)
			.ConfigureAwait(false);

		result.EnsureSuccessStatusCode();
	}

	public static async Task<TResponse> PutAsync<TResponse, TRequest>(
		this IRequestClient client,
		string requestUri,
		TRequest request,
		Dictionary<string, string> headers = null)
	{
		var requestString = JsonSerializer.Serialize(request);

		var result = await client
			.PutAsync(requestUri, requestString, "application/json", headers)
			.ConfigureAwait(false);

		var bodyBytes = result.EnsureSuccessStatusCode()
			.Body
			.ToArray();

		return JsonSerializer.Deserialize<TResponse>(bodyBytes, _options);
	}

	public static async Task PutAsync<TRequest>(
		this IRequestClient client,
		string requestUri,
		TRequest request, Dictionary<string, string> headers = null)
	{
		var requestString = JsonSerializer.Serialize(request);

		var result = await client
			.PutAsync(requestUri, requestString, "application/json", headers)
			.ConfigureAwait(false);

		result.EnsureSuccessStatusCode();
	}

	public static async Task<TResponse> DeleteAsync<TResponse>(
		this IRequestClient client,
		string requestUri, Dictionary<string, string> headers = null)
	{
		var result = await client
			.DeleteAsync(requestUri, headers)
			.ConfigureAwait(false);

		var bodyBytes = result.EnsureSuccessStatusCode()
			.Body
			.ToArray();

		return JsonSerializer.Deserialize<TResponse>(bodyBytes, _options);
	}
}