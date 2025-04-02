﻿using Cosei.Client.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace Cosei.Client.Http;

public class HttpRequestClient : IRequestClient
{
	private readonly HttpClient _client;

	public HttpRequestClient(string baseUrl)
	{
		_client = new HttpClient
		{
			BaseAddress = new Uri(baseUrl)
		};
	}

	public async Task<Response> GetAsync(string requestUri, Dictionary<string, string> headers = null)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

		AddHeaders(request, headers);

		using var response = await _client.SendAsync(request).ConfigureAwait(false);
		return await CreateResponseAsync(response).ConfigureAwait(false);
	}

	public async Task<Response> PostAsync(string requestUri, string request, string contentType, Dictionary<string, string> headers = null)
	{
		using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
		{
			Content = new StringContent(request, Encoding.UTF8, contentType)
		};

		AddHeaders(httpRequest, headers);

		using var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);
		return await CreateResponseAsync(response).ConfigureAwait(false);
	}

	public async Task<Response> PutAsync(string requestUri, string request, string contentType, Dictionary<string, string> headers = null)
	{
		using var httpRequest = new HttpRequestMessage(HttpMethod.Put, requestUri)
		{
			Content = new StringContent(request, Encoding.UTF8, contentType)
		};

		AddHeaders(httpRequest, headers);

		using var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);
		return await CreateResponseAsync(response).ConfigureAwait(false);
	}

	public async Task<Response> DeleteAsync(string requestUri, Dictionary<string, string> headers = null)
	{
		using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);

		AddHeaders(request, headers);

		using var response = await _client.SendAsync(request).ConfigureAwait(false);
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
		var array = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

		var responseContentType = response.Content.Headers.ContentType?.ToString();

		if (string.IsNullOrWhiteSpace(responseContentType) && response.Headers.TryGetValues("Content-Type", out var type))
		{
			responseContentType = type.FirstOrDefault();
		}

		responseContentType ??= MediaTypeNames.Text.Plain;

		return new Response((int)response.StatusCode, responseContentType, array);
	}

	private bool _disposedValue = false;

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			_disposedValue = true;

			if (disposing)
			{
				_client.Dispose();
			}
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}