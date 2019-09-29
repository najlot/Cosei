namespace Cosei.Service.RabbitMq
{
	public class RabbitMqConfiguration
	{
		public string Host { get; set; } = "";
		public string VirtualHost { get; set; } = "";
		public string UserName { get; set; } = "";
		public string Password { get; set; } = "";

		public string QueueName { get; set; } = "";
		public uint PrefetchSize { get; set; } = 0;
		public ushort PrefetchCount { get; set; } = 4;
	}
}