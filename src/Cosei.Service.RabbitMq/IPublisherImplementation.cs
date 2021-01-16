using System.Threading.Tasks;

namespace Cosei.Service.RabbitMq
{
	public interface IPublisherImplementation
	{
		Task PublishAsync(object message);
	}
}