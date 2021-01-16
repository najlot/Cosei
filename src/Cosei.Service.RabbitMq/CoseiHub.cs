using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Cosei.Service.RabbitMq
{
	public class CoseiHub : Hub, IPublisherImplementation
	{
		public async Task PublishAsync(object message)
		{
			if (Clients == null)
			{
				return;
			}

			var str = Newtonsoft.Json.JsonConvert.SerializeObject(message);
			await Clients.All.SendAsync(message.GetType().Name, str);
		}
	}
}