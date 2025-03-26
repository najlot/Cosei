using Cosei.Service.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cosei.Service.RabbitMq;

internal class RabbitMqConsumer(
	ILogger<RabbitMqConsumer> logger,
	IRabbitMqChannelFactory factory,
	RabbitMqConfiguration configuration,
	IServiceProvider serviceProvider,
	IRequestDelegateProvider delegateProvider) : BackgroundService
{
	private IChannel _channel;

	private readonly string _queueName = configuration.QueueName;
	private readonly uint _prefetchSize = configuration.PrefetchSize;
	private readonly ushort _prefetchCount = configuration.PrefetchCount;

	private readonly IRequestDelegateProvider _delegateProvider = delegateProvider;
	private readonly ILogger<RabbitMqConsumer> _logger = logger;
	private readonly IRabbitMqChannelFactory _factory = factory;
	private readonly IServiceProvider _serviceProvider = serviceProvider;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_channel = await _factory
			.CreateChannelAsync()
			.ConfigureAwait(false);

		await _channel
			.QueueDeclareAsync(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null)
			.ConfigureAwait(false);

		await _channel
			.BasicQosAsync(_prefetchSize, _prefetchCount, false)
			.ConfigureAwait(false);

		var consumer = new AsyncEventingBasicConsumer(_channel);
		consumer.ReceivedAsync += Consumer_Received;

		await _channel
			.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer)
			.ConfigureAwait(false);

		stoppingToken.Register(() =>
		{
			_channel?.Dispose();
			_channel = null;
		});
	}

	private Dictionary<string, string> ConvertHeaders(IDictionary<string, object> headers)
	{
		var dc = new Dictionary<string, string>(headers.Count);

		foreach (var header in headers)
		{
			var stringValue = Encoding.UTF8.GetString(header.Value as byte[]);
			dc.Add(header.Key, stringValue);
		}

		return dc;
	}

	private async Task Consumer_Received(object sender, BasicDeliverEventArgs ea)
	{
		var correlationId = ea.BasicProperties.CorrelationId;
		var replyProps = new BasicProperties
		{
			CorrelationId = correlationId,
			Headers = new Dictionary<string, object>()
		};

		using (_logger.BeginScope(correlationId))
		{
			try
			{
				var response = await GetResponse(ea);

				replyProps.Headers["StatusCode"] = response.HttpResponse.StatusCode;

				if (response.HttpResponse.ContentType != null)
				{
					replyProps.ContentType = response.HttpResponse.ContentType;
				}

				await _channel
					.BasicPublishAsync(exchange: "", routingKey: ea.BasicProperties.ReplyTo, mandatory: true, basicProperties: replyProps, body: response.Body)
					.ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				replyProps.Headers["StatusCode"] = 503;

				_logger.LogError(ex, "");

				var body = Encoding.UTF8.GetBytes("Internal server error.");
				await _channel
					.BasicPublishAsync(exchange: "", routingKey: ea.BasicProperties.ReplyTo, mandatory: true, basicProperties: replyProps, body: body)
					.ConfigureAwait(false);
			}
			finally
			{
				await _channel
					.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false)
					.ConfigureAwait(false);
			}
		}
	}

	private async Task<(HttpResponse HttpResponse, byte[] Body)> GetResponse(BasicDeliverEventArgs request)
	{
		byte[] body;
		var context = new DefaultHttpContext();
		var headers = ConvertHeaders(request.BasicProperties.Headers);

		if (!headers.TryGetValue("RequestUri", out var uri))
		{
			throw new Exception("RequestUri missing");
		}

		headers.Remove("RequestUri");

		if (!headers.TryGetValue("Method", out var method))
		{
			throw new Exception("Method missing");
		}

		headers.Remove("Method");

		foreach (var header in headers)
		{
			context.Request.Headers[header.Key] = header.Value;
		}

		using var requestStream = new MemoryStream(request.Body.ToArray());
		using var responseStream = new MemoryStream();
		context.Request.Body = requestStream;
		context.Response.Body = responseStream;

		context.Request.Path = uri.StartsWith("/") ? uri : "/" + uri;
		context.Request.Method = method;

		if (request.BasicProperties.ContentType != null)
		{
			context.Request.ContentType = request.BasicProperties.ContentType;
		}

		using (var scope = _serviceProvider.CreateScope())
		{
			context.RequestServices = scope.ServiceProvider;
			var requestDelegate = _delegateProvider.RequestDelegate;
			await requestDelegate(context);
		}

		context.Response.Body.Seek(0, SeekOrigin.Begin);

		using (var memoryStream = new MemoryStream())
		{
			context.Response.Body.CopyTo(memoryStream);
			body = memoryStream.ToArray();
		}

		return (context.Response, body);
	}
}