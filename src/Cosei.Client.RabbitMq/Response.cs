namespace Cosei.Client.RabbitMq
{
	public class Response
	{
		public string ContentType { get; internal set; }
		public int StatusCode { get; internal set; }
		public byte[] Body { get; internal set; }
	}
}