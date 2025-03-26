using Cosei.Service.Base;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cosei.Service.RabbitMq;

internal class RabbitMqPublisher : IPublisherImplementation, IDisposable
{
	private IChannel _channel;

	public RabbitMqPublisher(IRabbitMqChannelFactory factory)
	{
		this._factory = factory;
	}

	private readonly List<string> _declaredExchanges = new List<string>();
	private readonly IRabbitMqChannelFactory _factory;

	public async Task PublishAsync(Type type, string content)
	{
		_channel ??= await _factory
			.CreateChannelAsync()
			.ConfigureAwait(false);

		var body = Encoding.UTF8.GetBytes(content);

		var exchangeName = type.Name;

		if (!_declaredExchanges.Contains(exchangeName))
		{
			await _channel
				.ExchangeDeclareAsync(exchangeName, type: ExchangeType.Fanout)
				.ConfigureAwait(false);

			_declaredExchanges.Add(exchangeName);
		}

		await _channel
			.BasicPublishAsync(exchange: exchangeName, routingKey: "", body: body)
			.ConfigureAwait(false);
	}

	public Task PublishToUserAsync(string userId, Type type, string content)
	{
		throw new NotImplementedException();
	}

	private bool _disposedValue = false;

	protected void Dispose(bool disposing)
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
	}
}