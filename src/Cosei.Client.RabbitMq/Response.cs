using System;
using System.Text;

namespace Cosei.Client.RabbitMq
{
	public class Response
	{
		public string ContentType { get; internal set; }
		public int StatusCode { get; internal set; }
		public ReadOnlyMemory<byte> Body { get; internal set; }

		public Response(int statusCode, string contentType, ReadOnlyMemory<byte> body)
		{
			StatusCode = statusCode;
			ContentType = contentType;
			Body = body;
		}

		public Response EnsureSuccessStatusCode()
		{
			if (StatusCode >= 200 && StatusCode < 300)
			{
				return this;
			}

			throw new Exception(Encoding.UTF8.GetString(Body.ToArray()));
		}
	}
}