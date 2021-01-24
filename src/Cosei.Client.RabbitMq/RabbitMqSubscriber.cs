using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Cosei.Client.RabbitMq
{
	public class RabbitMqSubscriber : AbstractSubscriber, ISubscriber
	{
		private readonly IConnection _connection;
		private readonly IModel _channel;
		private readonly Action<AggregateException> _exceptionHandler;

		public RabbitMqSubscriber(string host, string virtualHost, string userName, string password, Action<AggregateException> exceptionHandler)
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
			_exceptionHandler = exceptionHandler;
		}

		public override async Task StartAsync()
		{
			await Task.Run(() =>
			{
				var _queueName = _channel.QueueDeclare().QueueName;
				var registrations = new List<(Type Type, MethodInfo MethodInfo)>();

				foreach (var type in GetRegisteredTypes())
				{
					var send = typeof(AbstractSubscriber)
						.GetMethod(nameof(SendAsync), BindingFlags.Instance | BindingFlags.NonPublic)
						.MakeGenericMethod(type);

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
							var message = Encoding.UTF8.GetString(e.Body.ToArray());
							var obj = JsonConvert.DeserializeObject(message, registration.Type);
							if (registration.MethodInfo.Invoke(this, new object[] { obj }) is Task task)
							{
								task.ContinueWith(faultedTask => _exceptionHandler(faultedTask.Exception), TaskContinuationOptions.OnlyOnFaulted);
							}
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

		protected override void Dispose(bool disposing)
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

		#endregion
	}
}