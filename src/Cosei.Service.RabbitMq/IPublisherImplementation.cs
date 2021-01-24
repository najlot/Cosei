﻿using System;
using System.Threading.Tasks;

namespace Cosei.Service.RabbitMq
{
	public interface IPublisherImplementation
	{
		Task PublishAsync(Type type, string content);
	}
}