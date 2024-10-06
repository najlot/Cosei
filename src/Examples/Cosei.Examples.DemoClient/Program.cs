using Cosei.Client.Base;
using Cosei.Client.Http;
using Cosei.Examples.DemoContracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Cosei.Examples.DemoClient
{
	internal class Handler
	{
		private readonly ISubscriber subscriber;

		public Handler()
		{
			subscriber = new SignalRSubscriber(
				"http://localhost:5000/cosei",
				ex => Console.WriteLine(ex.ToString())
				);
		}

		public async Task SubscribeAndStartAsync()
		{
			subscriber.Register<WeatherForecastsRequested>(HandleWeatherForecastsRequested);
			await subscriber.StartAsync();
		}

		public async Task StopAsync()
		{
			await subscriber.DisposeAsync();
		}

		private void HandleWeatherForecastsRequested(WeatherForecastsRequested message)
		{
			Console.WriteLine("Server got our request at " + message.RequestDate);
		}
	}

	internal class Program
	{
		private static async Task Main(string[] args)
		{
			// Subscribe start
			var handler = new Handler();
			await handler.SubscribeAndStartAsync();
			// Subscribe end

			while (true)
			{
				Console.WriteLine("Press any key to start or ESC to exit.");
				var key = Console.ReadKey();

				if (key.Key == ConsoleKey.Escape)
				{
					break;
				}

				var sw = Stopwatch.StartNew();

				// Request start
				using IRequestClient client = new HttpRequestClient("http://localhost:5000");
				var weatherForecasts = await client.GetAsync<List<WeatherForecast>>("/WeatherForecast");

				foreach (var forecast in weatherForecasts)
				{
					Console.WriteLine(forecast.Date + ": " + forecast.TemperatureC);
				}

				// Request end
				Console.WriteLine("Request finished in " + sw.Elapsed);
			}

			await handler.StopAsync();
		}
	}
}
