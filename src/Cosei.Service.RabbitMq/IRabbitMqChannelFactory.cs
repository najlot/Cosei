using RabbitMQ.Client;
using System.Threading.Tasks;

namespace Cosei.Service.RabbitMq;

public interface IRabbitMqChannelFactory
{
	Task<IChannel> CreateChannelAsync();
}