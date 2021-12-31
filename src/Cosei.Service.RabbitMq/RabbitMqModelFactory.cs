using RabbitMQ.Client;
using System;

namespace Cosei.Service.RabbitMq
{
	public class RabbitMqModelFactory : IDisposable, IRabbitMqModelFactory
	{
		private readonly IConnection _connection;

		public RabbitMqModelFactory(RabbitMqConfiguration configuration)
		{
			var factory = new ConnectionFactory()
			{
				HostName = configuration.Host,
				VirtualHost = configuration.VirtualHost,
				UserName = configuration.UserName,
				Password = configuration.Password
			};

			factory.AutomaticRecoveryEnabled = true;
			factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);

			_connection = factory.CreateConnection();
		}

		public IModel CreateModel()
		{
			return _connection.CreateModel();
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
}