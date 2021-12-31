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

namespace Cosei.Service.RabbitMq
{
	internal class RabbitMqConsumer : IHostedService, IDisposable
	{
		private IModel _channel;

		private readonly string _queueName;
		private readonly uint _prefetchSize;
		private readonly ushort _prefetchCount;

		private readonly IRequestDelegateProvider _delegateProvider;
		private readonly ILogger<RabbitMqConsumer> _logger;
		private readonly IRabbitMqModelFactory _factory;
		private readonly IServiceProvider _serviceProvider;

		public RabbitMqConsumer(
			ILogger<RabbitMqConsumer> logger,
			IRabbitMqModelFactory factory,
			RabbitMqConfiguration configuration,
			IServiceProvider serviceProvider,
			IRequestDelegateProvider delegateProvider)
		{
			_logger = logger;
			_factory = factory;
			_serviceProvider = serviceProvider;
			_delegateProvider = delegateProvider;
			
			_queueName = configuration.QueueName;
			_prefetchSize = configuration.PrefetchSize;
			_prefetchCount = configuration.PrefetchCount;
		}

		#region IHostedService

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			await Task.Run(() =>
			{
				_channel = _factory.CreateModel();
				_channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
				_channel.BasicQos(_prefetchSize, _prefetchCount, false);

				var consumer = new EventingBasicConsumer(_channel);
				consumer.Received += Consumer_Received;

				_channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
			});
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			await Task.Run(() =>
			{
				_channel?.Dispose();
				_channel = null;
			});
		}

		#endregion IHostedService

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

		private async void Consumer_Received(object sender, BasicDeliverEventArgs ea)
		{
			var correlationId = ea.BasicProperties.CorrelationId;
			var replyProps = _channel.CreateBasicProperties();
			replyProps.CorrelationId = correlationId;
			replyProps.Headers = new Dictionary<string, object>();

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

					_channel.BasicPublish(exchange: "", routingKey: ea.BasicProperties.ReplyTo, basicProperties: replyProps, body: response.Body);
				}
				catch (Exception ex)
				{
					replyProps.Headers["StatusCode"] = 503;

					_logger.LogError(ex, "");

					var body = Encoding.UTF8.GetBytes("Internal server error.");
					_channel.BasicPublish(exchange: "", routingKey: ea.BasicProperties.ReplyTo, basicProperties: replyProps, body: body);
				}
				finally
				{
					_channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
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
				context.Request.Headers.Add(header.Key, header.Value);
			}

			using (var requestStream = new MemoryStream(request.Body.ToArray()))
			using (var responseStream = new MemoryStream())
			{
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

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		protected void Dispose(bool disposing)
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
		}

		#endregion IDisposable Support
	}
}