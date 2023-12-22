using Cosei.Client.Base;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cosei.Client.Http
{
	public class SignalRSubscriber : AbstractSubscriber, ISubscriber
	{
		private readonly HubConnection _connection;
		private readonly Action<AggregateException> _exceptionHandler;
        private readonly List<Type> _connectedTypes = new List<Type>();

        public SignalRSubscriber(string url, Action<AggregateException> exceptionHandler)
			: this(url, _ => { }, exceptionHandler)
		{
		}

		public SignalRSubscriber(string url, Action<HttpConnectionOptions> configure, Action<AggregateException> exceptionHandler)
		{
			_connection = new HubConnectionBuilder()
				.WithUrl(url, configure)
				.WithAutomaticReconnect()
				.Build();

			_exceptionHandler = exceptionHandler;
		}

		public override async Task StartAsync()
		{
            var typesToConnect = GetRegisteredTypes()
                .Where(t => !_connectedTypes.Contains(t))
                .Distinct()
                .ToArray();

            var subscriberType = typeof(AbstractSubscriber);
            var method = subscriberType.GetMethod(nameof(SendAsync), BindingFlags.Instance | BindingFlags.NonPublic);

            if (method is null)
            {
                throw new NullReferenceException(nameof(method));
            }

            foreach (var type in typesToConnect)
			{
				var send = method.MakeGenericMethod(type);

				_connection.On<string>(type.Name, param =>
				{
					var obj = JsonSerializer.Deserialize(param, type);
					if (send.Invoke(this, new object[] { obj }) is Task task)
					{
						task.ContinueWith(faultedTask => _exceptionHandler(faultedTask.Exception), TaskContinuationOptions.OnlyOnFaulted);
					}
				});
			}

            if (_connection.State == HubConnectionState.Disconnected)
			{
                await _connection.StartAsync();
            }
		}

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		public override async Task DisposeAsync()
		{
			if (!disposedValue)
			{
				disposedValue = true;
				await _connection.DisposeAsync();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					DisposeAsync().Wait();
				}

				disposedValue = true;
			}
		}

		#endregion IDisposable Support
	}
}