using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cosei.Service.RabbitMq
{
	public static class EndpointRouteBuilderExtensions
	{
		public static HubEndpointConventionBuilder MapCoseiHub(this IEndpointRouteBuilder builder)
		{
			return builder.MapHub<SignalRHub>("/cosei");
		}
	}
}
