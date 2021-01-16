using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Cosei.Service.RabbitMq
{
	public static class ServiceCollectionExtensions
	{
		public static void AddPublisher<T>(this IServiceCollection services) where T : class, IPublisherImplementation
		{
			services.AddSingleton<T>();
			services.AddSingleton<IPublisherImplementation>(c => c.GetRequiredService<T>());
		}

		public static void AddCosei(this IServiceCollection services)
		{
			services.AddSingleton<RequestDelegateProvider>();
			services.AddPublisher<CoseiHub>();
			services.AddSingleton<IPublisher, Publisher>();
		}

		public static void AddCoseiRabbitMq(this IServiceCollection services, RabbitMqConfiguration rmqConfig)
		{
			services.AddSingleton(rmqConfig);
			services.AddPublisher<RabbitMqService>();
			services.AddSingleton<IHostedService>(c => c.GetRequiredService<RabbitMqService>());
		}

		public static void AddCoseiRabbitMq(this IServiceCollection services, Action<RabbitMqConfiguration> configure)
		{
			var rmqConfig = new RabbitMqConfiguration();
			configure(rmqConfig);
			services.AddCoseiRabbitMq(rmqConfig);
		}
	}
}