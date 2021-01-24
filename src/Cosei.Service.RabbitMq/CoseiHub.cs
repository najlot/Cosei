using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Cosei.Service.RabbitMq
{
	public class CoseiHub : Hub, IPublisherImplementation
	{
		public async Task PublishAsync(Type type, string content)
		{
			if (Clients == null)
			{
				return;
			}

			await Clients.All.SendAsync(type.Name, content);
		}
	}
}