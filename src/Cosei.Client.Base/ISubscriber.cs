using System;
using System.Threading.Tasks;

namespace Cosei.Client.Base;

public interface ISubscriber : IDisposable
{
	Task StartAsync();

	void Register<T>(Action<T> handler) where T : class;

	void Register<T>(Func<T, Task> handler) where T : class;

	void Unregister<T>(T obj) where T : class;

	Task DisposeAsync();
}