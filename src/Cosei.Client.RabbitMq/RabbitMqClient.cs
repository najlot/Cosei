using Cosei.Client.Base;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cosei.Client.RabbitMq;

public class RabbitMqClient : IRequestClient
{
	private readonly IRabbitMqChannelFactory _factory;
	private readonly string _requestQueueName;

	private bool isInitialized = false;
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	private IChannel _channel;
	private AsyncEventingBasicConsumer _consumer;
	private string _replyQueueName;

	public RabbitMqClient(IRabbitMqChannelFactory factory, string requestQueueName)
	{
		_factory = factory;
		_requestQueueName = requestQueueName;
	}

	public async Task<Response> PutAsync(string requestUri, string request, string contentType, Dictionary<string, string> headers = null)
	{
		return await RequestInternalAsync(requestUri, "PUT", request, contentType, true, headers);
	}

	public async Task<Response> PostAsync(string requestUri, string request, string contentType, Dictionary<string, string> headers = null)
	{
		return await RequestInternalAsync(requestUri, "POST", request, contentType, true, headers);
	}

	public async Task<Response> GetAsync(string requestUri, Dictionary<string, string> headers = null)
	{
		return await RequestInternalAsync(requestUri, "GET", null, null, false, headers);
	}

	public async Task<Response> DeleteAsync(string requestUri, Dictionary<string, string> headers = null)
	{
		return await RequestInternalAsync(requestUri, "DELETE", null, null, true, headers);
	}

	private async Task<Response> RequestInternalAsync(
		string requestUri,
		string method,
		string request,
		string contentType,
		bool persistent,
		Dictionary<string, string> headers)
	{
		if (!isInitialized)
		{
			await _semaphore.WaitAsync().ConfigureAwait(false);
			try
			{
				if (!isInitialized)
				{
					isInitialized = true;

					_channel = await _factory.CreateChannelAsync().ConfigureAwait(false);
					var queueDeclareResult = await _channel.QueueDeclareAsync().ConfigureAwait(false);
					_replyQueueName = queueDeclareResult.QueueName;
					_consumer = new AsyncEventingBasicConsumer(_channel);
				}
			}
			finally
			{
				_semaphore.Release();
			}
		}

		var taskCompletionSource = new TaskCompletionSource<Response>();
		var cancellationTokenSource = new CancellationTokenSource(60000);

		var correlationId = Guid.NewGuid().ToString();

		var properties = new BasicProperties
		{
			CorrelationId = correlationId,
			ReplyTo = _replyQueueName,

			Headers = new Dictionary<string, object>
			{
				{ "RequestUri", requestUri },
				{ "Method", method }
			}
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
			properties.DeliveryMode = DeliveryModes.Persistent;
		}

		Task consumerReceivedAsync(object sender, BasicDeliverEventArgs ea)
		{
			if (ea.BasicProperties.CorrelationId == correlationId)
			{
				_consumer.ReceivedAsync -= consumerReceivedAsync;

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

			return Task.CompletedTask;
		}

		_consumer.ReceivedAsync += consumerReceivedAsync;

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

			await _channel
				.BasicPublishAsync(exchange: "", routingKey: _requestQueueName, mandatory: true, basicProperties: properties, body: requestBytes)
				.ConfigureAwait(false);
		}
		else
		{
			await _channel
				.BasicPublishAsync(exchange: "", routingKey: _requestQueueName, mandatory: true, basicProperties: properties, body: null)
				.ConfigureAwait(false);
		}

		await _channel
			.BasicConsumeAsync(consumer: _consumer, queue: _replyQueueName, autoAck: true)
			.ConfigureAwait(false);

		return await taskCompletionSource.Task;
	}

	private bool _disposedValue = false;

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			_disposedValue = true;

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
}