namespace Cosei.Service.RabbitMq;

public class RabbitMqConfiguration
{
	public string Host { get; set; } = string.Empty;
	public string VirtualHost { get; set; } = string.Empty;
	public string UserName { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;

	public string QueueName { get; set; } = string.Empty;
	public uint PrefetchSize { get; set; } = 0;
	public ushort PrefetchCount { get; set; } = 4;
}