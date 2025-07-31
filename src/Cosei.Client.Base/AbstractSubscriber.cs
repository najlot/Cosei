using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Cosei.Client.Base;

public abstract class AbstractSubscriber : ISubscriber
{
	private struct TargetAndMethodInfo
	{
		public WeakReference<object> Target;
		public MethodInfo MethodInfo;
	}

	private readonly Dictionary<Type, List<TargetAndMethodInfo>> _registrations = [];

	protected IEnumerable<Type> GetRegisteredTypes()
	{
		foreach (var registration in _registrations)
		{
			yield return registration.Key;
		}
	}

	protected async Task SendAsync<T>(T message) where T : class
	{
		List<TargetAndMethodInfo> list;
		bool forceClean = false;

		lock (_registrations)
		{
			if (!_registrations.TryGetValue(typeof(T), out list))
			{
				return;
			}
		}

		TargetAndMethodInfo[] array;

		lock (list)
		{
			array = list.ToArray();
		}

		foreach (var entry in array)
		{
			if (entry.Target.TryGetTarget(out var target))
			{
				if (entry.MethodInfo.Invoke(target, [message]) is Task task)
				{
					await task;
				}
			}
			else
			{
				forceClean = true;
			}
		}

		if (forceClean)
		{
			lock (list)
			{
				list.RemoveAll(e => !e.Target.TryGetTarget(out var target));
			}
		}
	}

	public void Register<T>(Func<T, Task> handler) where T : class
	{
		var entry = new TargetAndMethodInfo()
		{
			Target = new WeakReference<object>(handler.Target),
			MethodInfo = handler.Method
		};

		Register(entry, typeof(T));
	}

	public void Register<T>(Action<T> handler) where T : class
	{
		var entry = new TargetAndMethodInfo()
		{
			Target = new WeakReference<object>(handler.Target),
			MethodInfo = handler.Method
		};

		Register(entry, typeof(T));
	}

	private void Register(TargetAndMethodInfo entry, Type type)
	{
		List<TargetAndMethodInfo> list;

		lock (_registrations)
		{
			if (!_registrations.TryGetValue(type, out list))
			{
				list = [];
				_registrations.Add(type, list);
			}
		}

		lock (list) list.Add(entry);
	}

	public void Unregister<T>(T obj) where T : class
	{
		var registrationsList = new List<List<TargetAndMethodInfo>>(_registrations.Count);

		lock (_registrations)
		{
			foreach (var entry in _registrations)
			{
				registrationsList.Add(entry.Value);
			}
		}

		foreach (var list in registrationsList)
		{
			lock (list) list.RemoveAll(e => (!e.Target.TryGetTarget(out var target)) || ReferenceEquals(target, obj));
		}
	}

	public abstract Task StartAsync();

	private bool _disposedValue = false;

	public virtual Task DisposeAsync(bool disposing)
	{
		Dispose(disposing);
		return Task.CompletedTask;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			_disposedValue = true;

			if (disposing)
			{
				_registrations.Clear();
			}
		}
	}

	public Task DisposeAsync() => DisposeAsync(true);
	public void Dispose() => Dispose(true);
}