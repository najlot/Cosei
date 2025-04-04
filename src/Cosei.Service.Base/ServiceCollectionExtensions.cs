﻿using Microsoft.Extensions.DependencyInjection;
using System;

namespace Cosei.Service.Base;

public static class ServiceCollectionExtensions
{
	private static bool AreBasicsAdded = false;

	public static void AddPublisher<T>(this IServiceCollection services) where T : class, IPublisherImplementation
	{
		if (!AreBasicsAdded)
		{
			AreBasicsAdded = true;

			services.AddSingleton<IRequestDelegateProvider, RequestDelegateProvider>();
			services.AddSingleton<IPublisher, Publisher>();
		}

		services.AddSingleton<T>();
		services.AddSingleton<Func<IPublisherImplementation>>(c => () => c.GetRequiredService<T>());
	}
}