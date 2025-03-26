using Cosei.Client.Base;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cosei.Client.RabbitMq;

public class RabbitMqSubscriber : AbstractSubscriber, ISubscriber
{
	private readonly IRabbitMqChannelFactory _factory;
	private readonly Action<AggregateException> _exceptionHandler;

	private IChannel _channel;
	private readonly List<Type> _connectedTypes = [];
	private bool _isConnected = false;

	public RabbitMqSubscriber(IRabbitMqChannelFactory factory, Action<AggregateException> exceptionHandler)
	{
		_factory = factory;
		_exceptionHandler = exceptionHandler;
	}

	public override async Task StartAsync()
	{
		_channel ??= await _factory.CreateChannelAsync();

		var typesToConnect = GetRegisteredTypes()
			.Where(t => !_connectedTypes.Contains(t))
			.Distinct()
			.ToArray();

		var subscriberType = typeof(AbstractSubscriber);
		var method = subscriberType.GetMethod(nameof(SendAsync), BindingFlags.Instance | BindingFlags.NonPublic);

		var declareResult = await _channel
			.QueueDeclareAsync()
			.ConfigureAwait(false);

		var queueName = declareResult.QueueName;
		var registrations = new List<(Type Type, MethodInfo MethodInfo)>();

		foreach (var type in typesToConnect)
		{
			var send = method.MakeGenericMethod(type);

			var registration = (type, send);
			registrations.Add(registration);

			await _channel
				.ExchangeDeclareAsync(type.Name, type: ExchangeType.Fanout)
				.ConfigureAwait(false);

			await _channel
				.QueueBindAsync(queue: queueName, exchange: type.Name, routingKey: "")
				.ConfigureAwait(false);

			_connectedTypes.Add(type);
		}

		var consumer = new AsyncEventingBasicConsumer(_channel);
		consumer.ReceivedAsync += (object sender, BasicDeliverEventArgs e) =>
		{
			foreach (var registration in registrations)
			{
				if (registration.Type.Name == e.Exchange)
				{
					var message = Encoding.UTF8.GetString(e.Body.ToArray());
					var obj = JsonSerializer.Deserialize(message, registration.Type);
					if (registration.MethodInfo.Invoke(this, [obj]) is Task task)
					{
						task.ContinueWith(faultedTask => _exceptionHandler(faultedTask.Exception), TaskContinuationOptions.OnlyOnFaulted);
					}
				}
			}

			return Task.CompletedTask;
		};

		if (!_isConnected)
		{
			_isConnected = true;

			await _channel
				.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer)
				.ConfigureAwait(false);
		}
	}

	private bool _disposedValue = false;

	public override Task DisposeAsync()
	{
		Dispose();
		return Task.CompletedTask;
	}

	protected override void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			_disposedValue = true;

			if (disposing)
			{
				_channel.Dispose();
			}
		}

		base.Dispose(disposing);
	}
}