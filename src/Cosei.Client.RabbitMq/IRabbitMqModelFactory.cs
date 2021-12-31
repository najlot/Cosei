using RabbitMQ.Client;

namespace Cosei.Client.RabbitMq
{
	public interface IRabbitMqModelFactory
	{
		IModel CreateModel();
	}
}