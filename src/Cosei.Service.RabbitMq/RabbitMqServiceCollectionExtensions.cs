using Cosei.Service.Base;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Cosei.Service.RabbitMq
{
	public static class RabbitMqServiceCollectionExtensions
	{
		public static void AddCoseiRabbitMq(this IServiceCollection services, RabbitMqConfiguration rmqConfig)
		{
			services.AddSingleton(rmqConfig);
			services.AddSingleton<IRabbitMqModelFactory, RabbitMqModelFactory>();
			services.AddPublisher<RabbitMqPublisher>();
			services.AddHostedService<RabbitMqConsumer>();
		}

		public static void AddCoseiRabbitMq(this IServiceCollection services, Action<RabbitMqConfiguration> configure)
		{
			var rmqConfig = new RabbitMqConfiguration();
			configure(rmqConfig);
			services.AddCoseiRabbitMq(rmqConfig);
		}
	}
}