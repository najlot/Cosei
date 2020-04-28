using System;
using System.Threading.Tasks;

namespace Cosei.Client.RabbitMq
{
	public interface ISubscriber : IDisposable
	{
		Task StartAsync();
		Task DisposeAsync();

		void Register<T>(Action<T> handler) where T : class;
		void Unregister<T>(T obj) where T : class;
	}
}