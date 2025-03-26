using System;
using System.Net.Http;
using System.Text;

namespace Cosei.Client.Base;

public class Response(int statusCode, string contentType, ReadOnlyMemory<byte> body)
{
	public string ContentType { get; internal set; } = contentType;
	public int StatusCode { get; internal set; } = statusCode;
	public ReadOnlyMemory<byte> Body { get; internal set; } = body;

	public Response EnsureSuccessStatusCode()
	{
		if (StatusCode >= 200 && StatusCode < 300)
		{
			return this;
		}

		var errorText = Encoding.UTF8.GetString(Body.ToArray());

		if (string.IsNullOrWhiteSpace(errorText))
		{
			errorText = StatusCode switch
			{
				400 => "Bad Request",
				401 => "Unauthorized",
				403 => "Forbidden",
				404 => "Not Found",
				500 => "Internal Server Error",
				_ => $"Unexpected status code: {StatusCode}",
			};
		}

		if (StatusCode == 401)
		{
			throw new System.Security.Authentication.AuthenticationException(errorText);
		}

		throw new HttpRequestException(errorText);
	}
}