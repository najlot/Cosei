using System.Threading.Tasks;

namespace Cosei.Service.RabbitMq
{
	public interface IPublisher
	{
		Task PublishAsync(object message);
	}
}