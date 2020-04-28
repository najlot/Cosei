using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cosei.Client.RabbitMq
{
	public class RabbitMqSubscriber : AbstractSubscriber, ISubscriber
	{
		private readonly IConnection _connection;
		private readonly IModel _channel;

		public RabbitMqSubscriber(string host, string virtualHost, string userName, string password)
		{
			var factory = new ConnectionFactory()
			{
				HostName = host,
				VirtualHost = virtualHost,
				UserName = userName,
				Password = password
			};

			factory.AutomaticRecoveryEnabled = true;
			factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);

			_connection = factory.CreateConnection();
			_channel = _connection.CreateModel();
		}

		public override async Task StartAsync()
		{
			await Task.Run(() =>
			{
				var _queueName = _channel.QueueDeclare().QueueName;
				List<(Type Type, MethodInfo MethodInfo)> registrations = new List<(Type, MethodInfo)>();

				foreach (var type in GetRegisteredTypes())
				{
					var send = typeof(AbstractSubscriber).GetMethod("Send", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(type);
					var registration = (type, send);
					registrations.Add(registration);

					_channel.ExchangeDeclare(type.Name, type: ExchangeType.Fanout);
					_channel.QueueBind(queue: _queueName, exchange: type.Name, routingKey: "");
				}

				var consumer = new EventingBasicConsumer(_channel);
				consumer.Received += (object sender, BasicDeliverEventArgs e) =>
				{
					foreach(var registration in registrations)
					{
						if (registration.Type.Name == e.Exchange)
						{
							var message = Encoding.UTF8.GetString(e.Body);
							var obj = Newtonsoft.Json.JsonConvert.DeserializeObject(message, registration.Type);
							registration.MethodInfo.Invoke(this, new object[] { obj });
						}
					}
				};

				_channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);
			});
		}

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		public override Task DisposeAsync()
		{
			if (!disposedValue)
			{
				disposedValue = true;
				_connection.Dispose();
			}

			return Task.CompletedTask;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				disposedValue = true;

				if (disposing)
				{
					_connection.Dispose();
				}
			}
		}

		// This code added to correctly implement the disposable pattern.
		public override void Dispose()
		{
			Dispose(true);
		}

		#endregion
	}
}