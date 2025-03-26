using System.Threading.Tasks;

namespace Cosei.Service.Base;

public interface IPublisher
{
	Task PublishAsync(object message);

	Task PublishToUserAsync(string userId, object message);
}