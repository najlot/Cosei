using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cosei.Client.RabbitMq
{
	public class RabbitMqClient : IDisposable, IRequestClient
	{
		private IModel Channel;
		private IConnection Connection;
		private readonly EventingBasicConsumer Consumer;
		private readonly string ReplyQueueName;
		private readonly string RequestQueueName;

		public Dictionary<string, string> DefaultHeaders { get; } = new Dictionary<string, string>();

		public RabbitMqClient(
			string hostName, string virtualHost,
			string userName, string password,
			string requestQueueName)
		{
			var factory = new ConnectionFactory()
			{
				HostName = hostName,
				VirtualHost = virtualHost,
				UserName = userName,
				Password = password
			};

			Connection = factory.CreateConnection();
			Channel = Connection.CreateModel();
			ReplyQueueName = Channel.QueueDeclare().QueueName;
			Consumer = new EventingBasicConsumer(Channel);
			RequestQueueName = requestQueueName;
		}

		public async Task<Response> PutAsync(string requestUri, string request, string contentType)
		{
			return await ResuestInternalAsync(requestUri, "PUT", request, contentType, true);
		}

		public async Task<Response> PostAsync(string requestUri, string request, string contentType)
		{
			return await ResuestInternalAsync(requestUri, "POST", request, contentType, true);
		}

		public async Task<Response> GetAsync(string requestUri)
		{
			return await ResuestInternalAsync(requestUri, "GET", null, null, false);
		}

		public async Task<Response> DeleteAsync(string requestUri)
		{
			return await ResuestInternalAsync(requestUri, "DELETE", null, null, true);
		}

		private Task<Response> ResuestInternalAsync(string requestUri, string method, string request, string contentType, bool persistent)
		{
			var taskCompletionSource = new TaskCompletionSource<Response>();
			var resultTask = taskCompletionSource.Task;

			var correlationId = Guid.NewGuid().ToString();

			IBasicProperties properties = Channel.CreateBasicProperties();
			properties.CorrelationId = correlationId;
			properties.ReplyTo = ReplyQueueName;

			var headers = new Dictionary<string, object>
			{
				{ "RequestUri", requestUri },
				{ "Method", method }
			};

			foreach (var header in DefaultHeaders)
			{
				headers.Add(header.Key, header.Value);
			}

			properties.Headers = headers;

			if (persistent)
			{
				properties.DeliveryMode = 2;
			}

			void handler(object model, BasicDeliverEventArgs ea)
			{
				if (ea.BasicProperties.CorrelationId == correlationId)
				{
					Consumer.Received -= handler;

					try
					{
						var response = new Response(
							(int)ea.BasicProperties.Headers["StatusCode"],
							ea.BasicProperties.ContentType,
							ea.Body);

						taskCompletionSource.SetResult(response);
					}
					catch (Exception ex)
					{
						taskCompletionSource.SetException(ex);
					}
				}
			}

			Consumer.Received += handler;

			var cancellationTokenSource = new CancellationTokenSource(60000);
			cancellationTokenSource.Token.Register(() =>
			{
				if (!taskCompletionSource.Task.IsCompleted)
				{
					taskCompletionSource.SetCanceled();
				}
			}, false);

			if (request != null)
			{
				byte[] requestBytes = Encoding.UTF8.GetBytes(request);
				properties.ContentType = contentType;
				Channel.BasicPublish(exchange: "", routingKey: RequestQueueName, basicProperties: properties, body: requestBytes);
			}
			else
			{
				Channel.BasicPublish(exchange: "", routingKey: RequestQueueName, basicProperties: properties, body: null);
			}

			Channel.BasicConsume(consumer: Consumer, queue: ReplyQueueName, autoAck: true);

			return resultTask;
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
					Channel?.Dispose();
					Channel = null;
					Connection?.Close();
					Connection?.Dispose();
					Connection = null;
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