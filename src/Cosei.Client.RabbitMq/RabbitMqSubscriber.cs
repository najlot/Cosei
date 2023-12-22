using Cosei.Client.Base;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Cosei.Client.RabbitMq
{
	public class RabbitMqSubscriber : AbstractSubscriber, ISubscriber
	{
		private readonly IModel _channel;
		private readonly Action<AggregateException> _exceptionHandler;
        private readonly List<Type> _connectedTypes = new List<Type>();
        private bool _isConnected = false;

        public RabbitMqSubscriber(IRabbitMqModelFactory factory, Action<AggregateException> exceptionHandler)
		{
			_exceptionHandler = exceptionHandler;
			_channel = factory.CreateModel();
		}

		public override async Task StartAsync()
		{
            var typesToConnect = GetRegisteredTypes()
                .Where(t => !_connectedTypes.Contains(t))
                .Distinct()
                .ToArray();

            var subscriberType = typeof(AbstractSubscriber);
			var method = subscriberType.GetMethod(nameof(SendAsync), BindingFlags.Instance | BindingFlags.NonPublic);

            await Task.Run(() =>
			{
				var _queueName = _channel.QueueDeclare().QueueName;
				var registrations = new List<(Type Type, MethodInfo MethodInfo)>();

				foreach (var type in typesToConnect)
				{
					var send = method.MakeGenericMethod(type);

					var registration = (type, send);
					registrations.Add(registration);

					_channel.ExchangeDeclare(type.Name, type: ExchangeType.Fanout);
					_channel.QueueBind(queue: _queueName, exchange: type.Name, routingKey: "");

					_connectedTypes.Add(type);
                }

				var consumer = new EventingBasicConsumer(_channel);
				consumer.Received += (object sender, BasicDeliverEventArgs e) =>
				{
					foreach (var registration in registrations)
					{
						if (registration.Type.Name == e.Exchange)
						{
							var message = Encoding.UTF8.GetString(e.Body.ToArray());
							var obj = JsonSerializer.Deserialize(message, registration.Type);
							if (registration.MethodInfo.Invoke(this, new object[] { obj }) is Task task)
							{
								task.ContinueWith(faultedTask => _exceptionHandler(faultedTask.Exception), TaskContinuationOptions.OnlyOnFaulted);
                            }
                        }
                    }
				};

                if (!_isConnected)
				{
                    _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);
					_isConnected = true;
                }
			});
		}

		#region IDisposable Support

		private bool _disposedValue = false; // To detect redundant calls

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

		#endregion IDisposable Support
	}
}