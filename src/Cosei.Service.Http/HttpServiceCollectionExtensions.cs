using Cosei.Service.Base;
using Microsoft.Extensions.DependencyInjection;

namespace Cosei.Service.Http
{
	public static class HttpServiceCollectionExtensions
	{
		public static void AddCoseiHttp(this IServiceCollection services)
		{
			services.AddPublisher<CoseiHub>();
		}
	}
}