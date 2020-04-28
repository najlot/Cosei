using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Cosei.Service.RabbitMq
{
	internal class SignalRHub : Hub
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