using RabbitMQ.Client;
using System;
using System.Threading.Tasks;

namespace Cosei.Client.RabbitMq;

public class RabbitMqModelFactory : IRabbitMqChannelFactory, IDisposable
{
	private IConnection _connection = null;
	private readonly ConnectionFactory _connectionFactory;

	public RabbitMqModelFactory(string host, string virtualHost, string userName, string password)
	{
		_connectionFactory = new ConnectionFactory
		{
			HostName = host,
			VirtualHost = virtualHost,
			UserName = userName,
			Password = password,
			AutomaticRecoveryEnabled = true,
			NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
		};
	}

	public async Task<IChannel> CreateChannelAsync()
	{
		_connection ??= await _connectionFactory
				.CreateConnectionAsync()
				.ConfigureAwait(false);

		return await _connection
			.CreateChannelAsync()
			.ConfigureAwait(false);
	}

	private bool _disposedValue;

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			_disposedValue = true;

			if (disposing)
			{
				_connection.Dispose();
			}
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}