using Cosei.Client.RabbitMq;
using Cosei.Examples.DemoContracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;

namespace Cosei.Examples.DemoClient
{
	class Handler
	{
		ISubscriber subscriber;

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

	class Program
	{
		static async Task Main(string[] args)
		{
			// Subscribe start
			var handler = new Handler();
			await handler.SubscribeAndStartAsync();
			// Subscribe end

			// Request start
			using var client = new HttpRequestClient("http://localhost:5000");
			var forecastsResult = await client.GetAsync("/WeatherForecast");
			forecastsResult = forecastsResult.EnsureSuccessStatusCode();
			var arr = forecastsResult.Body.ToArray();
			var str = Encoding.UTF8.GetString(arr);
			var weatherForecasts = JsonConvert.DeserializeObject<List<WeatherForecast>>(str);
			
			foreach (var forecast in weatherForecasts)
			{
				Console.WriteLine(forecast.Date + ": " + forecast.TemperatureC);
			}
			// Request end

			Console.ReadKey();

			await handler.StopAsync();
		}
	}
}
