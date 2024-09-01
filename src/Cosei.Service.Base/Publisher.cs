using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cosei.Service.Base
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
			if (_implementations.Length == 0 || message == null)
			{
				return;
			}

			var type = message.GetType();
			var content = JsonSerializer.Serialize(message);

			foreach (var implementation in _implementations)
			{
				await implementation.PublishAsync(type, content);
			}
		}

		public async Task PublishToUserAsync(string userId, object message)
		{
			if (_implementations.Length == 0 || message == null)
			{
				return;
			}

			var type = message.GetType();
			var content = JsonSerializer.Serialize(message);

			foreach (var implementation in _implementations)
			{
				await implementation.PublishToUserAsync(userId, type, content);
			}
		}
	}
}