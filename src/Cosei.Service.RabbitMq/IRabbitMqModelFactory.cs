using RabbitMQ.Client;

namespace Cosei.Service.RabbitMq
{
	public interface IRabbitMqModelFactory
	{
		IModel CreateModel();
	}
}