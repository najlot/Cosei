using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Cosei.Client.RabbitMq
{
	public class SignalRSubscriber : AbstractSubscriber, ISubscriber
	{
		private readonly HubConnection _connection;

		public SignalRSubscriber(string url)
			: this(url, _ => { })
		{
		}

		public SignalRSubscriber(string url, Action<HttpConnectionOptions> configure)
		{
			_connection = new HubConnectionBuilder()
				.WithUrl(url, configure)
				.WithAutomaticReconnect()
				.Build();
		}

		public override async Task StartAsync()
		{
			foreach (var type in GetRegisteredTypes())
			{
				var send = typeof(AbstractSubscriber).GetMethod("Send", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(type);

				_connection.On<string>(type.Name, param =>
				{
					var obj = Newtonsoft.Json.JsonConvert.DeserializeObject(param, type);
					send.Invoke(this, new object[] { obj });
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