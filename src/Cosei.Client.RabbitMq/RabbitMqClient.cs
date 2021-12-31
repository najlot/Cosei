using Cosei.Client.Base;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cosei.Client.RabbitMq
{
	public class RabbitMqClient : IRequestClient
	{
		private IModel _channel;
		private readonly EventingBasicConsumer _consumer;
		private readonly string _replyQueueName;
		private readonly string _requestQueueName;

		public RabbitMqClient(IRabbitMqModelFactory factory, string requestQueueName)
		{
			_requestQueueName = requestQueueName;
			_channel = factory.CreateModel();
			_replyQueueName = _channel.QueueDeclare().QueueName;
			_consumer = new EventingBasicConsumer(_channel);
		}

		public async Task<Response> PutAsync(string requestUri, string request, string contentType, Dictionary<string, string> headers = null)
		{
			return await ResuestInternalAsync(requestUri, "PUT", request, contentType, true, headers);
		}

		public async Task<Response> PostAsync(string requestUri, string request, string contentType, Dictionary<string, string> headers = null)
		{
			return await ResuestInternalAsync(requestUri, "POST", request, contentType, true, headers);
		}

		public async Task<Response> GetAsync(string requestUri, Dictionary<string, string> headers = null)
		{
			return await ResuestInternalAsync(requestUri, "GET", null, null, false, headers);
		}

		public async Task<Response> DeleteAsync(string requestUri, Dictionary<string, string> headers = null)
		{
			return await ResuestInternalAsync(requestUri, "DELETE", null, null, true, headers);
		}

		private async Task<Response> ResuestInternalAsync(string requestUri, string method, string request, string contentType, bool persistent, Dictionary<string, string> headers)
		{
			var taskCompletionSource = new TaskCompletionSource<Response>();
			var cancellationTokenSource = new CancellationTokenSource(60000);

			var correlationId = Guid.NewGuid().ToString();

			IBasicProperties properties = _channel.CreateBasicProperties();
			properties.CorrelationId = correlationId;
			properties.ReplyTo = _replyQueueName;

			properties.Headers = new Dictionary<string, object>
			{
				{ "RequestUri", requestUri },
				{ "Method", method }
			};

			if (headers != null)
			{
				foreach (var header in headers)
				{
					properties.Headers.Add(header.Key, header.Value);
				}
			}

			if (persistent)
			{
				properties.DeliveryMode = 2;
			}

			void handler(object model, BasicDeliverEventArgs ea)
			{
				if (ea.BasicProperties.CorrelationId == correlationId)
				{
					_consumer.Received -= handler;

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

			_consumer.Received += handler;

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
				_channel.BasicPublish(exchange: "", routingKey: _requestQueueName, basicProperties: properties, body: requestBytes);
			}
			else
			{
				_channel.BasicPublish(exchange: "", routingKey: _requestQueueName, basicProperties: properties, body: null);
			}

			_channel.BasicConsume(consumer: _consumer, queue: _replyQueueName, autoAck: true);

			return await taskCompletionSource.Task;
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
					_channel?.Dispose();
					_channel = null;
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