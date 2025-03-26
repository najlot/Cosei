using Cosei.Client.Base;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cosei.Client.Http;

public class SignalRSubscriber(
	string url,
	Action<HttpConnectionOptions> configure,
	Action<AggregateException> exceptionHandler) : AbstractSubscriber, ISubscriber
{
	private readonly HubConnection _connection = new HubConnectionBuilder()
			.WithUrl(url, configure)
			.WithAutomaticReconnect()
			.Build();

	private readonly Action<AggregateException> _exceptionHandler = exceptionHandler;
	private readonly List<Type> _connectedTypes = [];

	public SignalRSubscriber(string url, Action<AggregateException> exceptionHandler)
	: this(url, _ => { }, exceptionHandler)
	{
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
			_connectedTypes.Add(type);

			var send = method.MakeGenericMethod(type);

			_connection.On<string>(type.Name, param =>
			{
				var obj = JsonSerializer.Deserialize(param, type);
				if (send.Invoke(this, [obj]) is Task task)
				{
					task.ContinueWith(faultedTask => _exceptionHandler(faultedTask.Exception), TaskContinuationOptions.OnlyOnFaulted);
				}
			});
		}

		if (_connection.State == HubConnectionState.Disconnected)
		{
			await _connection.StartAsync().ConfigureAwait(false);
		}
	}

	#region IDisposable Support

	private bool _disposedValue = false;

	public override async Task DisposeAsync()
	{
		if (!_disposedValue)
		{
			_disposedValue = true;
			await _connection.DisposeAsync();
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				DisposeAsync().Wait();
			}

			_disposedValue = true;
		}
	}

	#endregion IDisposable Support
}