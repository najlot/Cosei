﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Cosei.Client.Base
{
	public abstract class AbstractSubscriber : ISubscriber
	{
		private struct TargetAndMethodInfo
		{
			public WeakReference<object> Target;
			public MethodInfo MethodInfo;
		}

		private readonly Dictionary<Type, List<TargetAndMethodInfo>> _registrations = new Dictionary<Type, List<TargetAndMethodInfo>>();

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

			foreach (var entry in list)
			{
				if (entry.Target.TryGetTarget(out var target))
				{
					if (entry.MethodInfo.Invoke(target, new object[] { message }) is Task task)
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
				lock (list) list.RemoveAll(e => !e.Target.TryGetTarget(out var target));
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
					list = new List<TargetAndMethodInfo>();
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

		public abstract Task DisposeAsync();

		public abstract Task StartAsync();

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				disposedValue = true;

				if (disposing)
				{
					_registrations.Clear();
				}
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			Dispose(true);
		}

		#endregion IDisposable Support
	}
}