using System.Threading.Tasks;

namespace Cosei.Service.RabbitMq
{
	internal class Publisher : IPublisher
	{
		private readonly SignalRHub _signalRHub;
		private readonly RabbitMqService _rabbitMqService;

		public Publisher(SignalRHub signalRHub, RabbitMqService rabbitMqService)
		{
			_signalRHub = signalRHub;
			_rabbitMqService = rabbitMqService;
		}

		public async Task PublishAsync(object message)
		{
			await _signalRHub.PublishAsync(message);
			await _rabbitMqService.PublishAsync(message);
		}
	}
}