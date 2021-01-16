﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cosei.Service.RabbitMq
{
	internal class Publisher : IPublisher
	{
		private readonly IPublisherImplementation[] _implementations;

		public Publisher(IEnumerable<Func<IPublisherImplementation>> implementations)
		{
			_implementations = implementations
				.Select(f => f())
				.Distinct()
				.ToArray();
		}

		public async Task PublishAsync(object message)
		{
			foreach (var implementation in _implementations)
			{
				await implementation.PublishAsync(message);
			}
		}
	}
}