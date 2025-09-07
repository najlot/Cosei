# gRPC Transport Usage Examples

This document demonstrates how to use the Cosei gRPC transport implementation.

## Server Setup (ASP.NET Core with gRPC)

### 1. Install NuGet Package
```xml
<PackageReference Include="Cosei.Service.Grpc" Version="0.1.2" />
```

### 2. Configure Services in Startup.cs or Program.cs

```csharp
using Cosei.Service.Grpc;

public void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();
    services.AddCoseiGrpc(); // Adds gRPC support and Cosei publisher
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseRouting();
    
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapGrpcService<CoseiGrpcService>(); // Map the gRPC service
    });

    app.UseCosei(); // Enable Cosei request processing
}
```

### 3. Example Controller

```csharp
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly IPublisher _publisher;

    public WeatherForecastController(IPublisher publisher)
    {
        _publisher = publisher;
    }

    [HttpGet]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        var forecasts = GetWeatherForecasts();
        
        // Publish a message when forecasts are requested
        await _publisher.PublishAsync(new WeatherForecastsRequested 
        { 
            RequestDate = DateTime.Now 
        });
        
        return forecasts;
    }
}
```

## Client Usage

### 1. Install NuGet Package
```xml
<PackageReference Include="Cosei.Client.Grpc" Version="0.1.2" />
```

### 2. Basic Request Client Usage

```csharp
using Cosei.Client.Grpc;
using Cosei.Client.Base;

// Create a gRPC client
using IRequestClient client = new GrpcRequestClient("https://localhost:5001");

// Make requests
var weatherForecasts = await client.GetAsync<List<WeatherForecast>>("/WeatherForecast");

// POST with data
var newForecast = new WeatherForecast { Date = DateTime.Now, TemperatureC = 25 };
await client.PostAsync("/WeatherForecast", newForecast);

// PUT and DELETE also supported
await client.PutAsync("/WeatherForecast/1", updatedForecast);
await client.DeleteAsync("/WeatherForecast/1");
```

### 3. Subscriber Usage for Streaming Messages

```csharp
using Cosei.Client.Grpc;

// Create subscriber for receiving published messages
var subscriber = new GrpcSubscriber("https://localhost:5001");

// Register message handlers
subscriber.Register<WeatherForecastsRequested>(message =>
{
    Console.WriteLine($"Weather forecasts requested at: {message.RequestDate}");
});

// Start listening for messages
await subscriber.StartAsync();

// When done, dispose the subscriber
await subscriber.DisposeAsync();
```

### 4. User-Specific Subscriptions

```csharp
// Subscribe only to messages for a specific user
var userSubscriber = new GrpcSubscriber("https://localhost:5001", userId: "user123");

subscriber.Register<UserNotification>(notification =>
{
    Console.WriteLine($"Notification for user: {notification.Message}");
});

await subscriber.StartAsync();
```

## Configuration Options

### Server Configuration

The gRPC service can be configured with standard gRPC options:

```csharp
services.AddGrpc(options =>
{
    options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4MB
    options.MaxSendMessageSize = 4 * 1024 * 1024;    // 4MB
    options.EnableDetailedErrors = true;
});

services.AddCoseiGrpc();
```

### Client Configuration

For advanced gRPC client configuration:

```csharp
var channel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions
{
    MaxReceiveMessageSize = 4 * 1024 * 1024,
    MaxSendMessageSize = 4 * 1024 * 1024
});

using var client = new GrpcRequestClient(channel);
```

## Performance Benefits

The gRPC transport offers several advantages:

1. **High Performance**: Binary protocol with efficient serialization
2. **HTTP/2**: Multiplexed connections, server push, header compression  
3. **Streaming**: Built-in support for bidirectional streaming
4. **Type Safety**: Strong typing with Protocol Buffers
5. **Cross-Platform**: Works across different languages and platforms
6. **Deadlines & Cancellation**: Built-in timeout and cancellation support

## Protocol Definition

The gRPC service uses this protocol definition (`cosei.proto`):

```protobuf
syntax = "proto3";

service CoseiService {
  rpc ProcessRequest (RequestMessage) returns (ResponseMessage);
  rpc Subscribe (SubscriptionRequest) returns (stream ResponseMessage);
}

message RequestMessage {
  string method = 1;           // GET, POST, PUT, DELETE
  string request_uri = 2;      // The request URI/path
  string content_type = 3;     // Content type for body
  bytes body = 4;              // Request body
  map<string, string> headers = 5; // Additional headers
}

message ResponseMessage {
  int32 status_code = 1;       // HTTP status code
  string content_type = 2;     // Response content type
  bytes body = 3;              // Response body
  map<string, string> headers = 4; // Response headers
}
```

This allows the gRPC transport to handle HTTP-like operations while maintaining compatibility with existing Cosei patterns.