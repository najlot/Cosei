using Microsoft.Extensions.DependencyInjection;
using System;

namespace Cosei.Service.RabbitMq
{
	public static class ServiceCollectionExtensions
	{
		public static void AddCosei(this IServiceCollection services, RabbitMqConfiguration rmqConfig)
		{
			services.AddSingleton(rmqConfig);

			services.AddSingleton<RequestDelegateProvider>();
			services.AddHostedService<RabbitMqService>();
		}

		public static void AddCosei(this IServiceCollection services, Action<RabbitMqConfiguration> configure)
		{
			var rmqConfig = new RabbitMqConfiguration();
			configure(rmqConfig);
			services.AddSingleton(rmqConfig);

			services.AddSingleton<RequestDelegateProvider>();
			services.AddHostedService<RabbitMqService>();
		}
	}
}