using Cosei.Examples.DemoContracts;
using Cosei.Service.RabbitMq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cosei.Examples.DemoService.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class WeatherForecastController : ControllerBase
	{
		private static readonly string[] Summaries = new[]
		{
			"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		private readonly ILogger<WeatherForecastController> _logger;
		private readonly IPublisher _publisher;

		public WeatherForecastController(ILogger<WeatherForecastController> logger, IPublisher publisher)
		{
			_logger = logger;
			_publisher = publisher;
		}

		[HttpGet]
		public async Task<List<WeatherForecast>> Get()
		{
			await _publisher.PublishAsync(new WeatherForecastsRequested
			{
				RequestDate = DateTime.Now
			});

			var rng = new Random();
			return Enumerable.Range(1, 5).Select(index => new WeatherForecast
			{
				Date = DateTime.Now.AddDays(index),
				TemperatureC = rng.Next(-20, 55),
				Summary = Summaries[rng.Next(Summaries.Length)]
			})
			.ToList();
		}
	}
}
