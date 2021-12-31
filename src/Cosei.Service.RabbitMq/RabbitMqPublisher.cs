using Cosei.Service.Base;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cosei.Service.RabbitMq
{
	internal class RabbitMqPublisher : IPublisherImplementation, IDisposable
	{
		private IModel _channel;

		public RabbitMqPublisher(IRabbitMqModelFactory factory)
		{
			_channel = factory.CreateModel();
		}

		private readonly List<string> _declaredExchanges = new List<string>();

		public async Task PublishAsync(Type type, string content)
		{
			await Task.Run(() =>
			{
				var body = Encoding.UTF8.GetBytes(content);

				var exchangeName = type.Name;

				if (!_declaredExchanges.Contains(exchangeName))
				{
					_channel.ExchangeDeclare(exchangeName, type: ExchangeType.Fanout);
					_declaredExchanges.Add(exchangeName);
				}

				_channel.BasicPublish(exchange: exchangeName, routingKey: "", basicProperties: null, body: body);
			});
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