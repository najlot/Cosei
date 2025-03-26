using RabbitMQ.Client;
using System.Threading.Tasks;

namespace Cosei.Client.RabbitMq;

public interface IRabbitMqChannelFactory
{
	Task<IChannel> CreateChannelAsync();
}