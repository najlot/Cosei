using System;
using System.Threading.Tasks;

namespace Cosei.Service.Base
{
	public interface IPublisherImplementation
	{
		Task PublishAsync(Type type, string content);
	}
}