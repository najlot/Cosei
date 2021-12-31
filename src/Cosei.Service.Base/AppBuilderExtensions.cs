using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cosei.Service.Base
{
	public static class AppBuilderExtensions
	{
		public static IApplicationBuilder UseCosei(this IApplicationBuilder app)
		{
			var services = app.ApplicationServices;
			var accessor = services.GetRequiredService<IRequestDelegateProvider>() as RequestDelegateProvider;
			accessor.SetApplicationBuilder(app);
			return app;
		}
	}
}
