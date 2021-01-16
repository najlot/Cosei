using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cosei.Client.RabbitMq
{
	public class SignalRSubscriber : AbstractSubscriber, ISubscriber
	{
		private readonly HubConnection _connection;
		private readonly Action<AggregateException> _exceptionHandler;

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
			foreach (var type in GetRegisteredTypes())
			{
				var send = typeof(AbstractSubscriber).GetMethod(nameof(SendAsync), BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(type);

				_connection.On<string>(type.Name, param =>
				{
					var obj = JsonSerializer.Deserialize(param, type);
					if (send.Invoke(this, new object[] { obj }) is Task task)
					{
						task.ContinueWith(faultedTask => _exceptionHandler(faultedTask.Exception), TaskContinuationOptions.OnlyOnFaulted);
					}
				});
			}

			await _connection.StartAsync();
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
				disposedValue = true;

				if (disposing)
				{
					_connection.DisposeAsync().Wait();
				}
			}
		}

		#endregion
	}
}