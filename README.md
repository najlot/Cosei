# Cosei

Call ASP.NET Core Controllers from other sources.

## Supported Transports

Cosei provides flexibility in how you expose and consume controller endpoints beyond traditional HTTP requests:

- **HTTP**: Standard HTTP client with SignalR for real-time messaging
- **RabbitMQ**: Message queue-based communication for distributed scenarios  
- **gRPC**: High-performance binary protocol with streaming support

## Quick Start

### Server Setup
```csharp
// Choose your transport
services.AddCoseiHttp();     // For HTTP + SignalR
services.AddCoseiRabbitMq(); // For RabbitMQ
services.AddCoseiGrpc();     // For gRPC
```

### Client Usage
```csharp
// HTTP
using IRequestClient client = new HttpRequestClient("http://localhost:5000");

// RabbitMQ  
using IRequestClient client = new RabbitMqClient(factory, "queue-name");

// gRPC
using IRequestClient client = new GrpcRequestClient("https://localhost:5001");

// All transports support the same interface
var data = await client.GetAsync<List<WeatherForecast>>("/WeatherForecast");
```

## Documentation

- [gRPC Transport Usage](docs/GRPC_USAGE.md)

## Features

- **Multiple Transport Options**: HTTP, RabbitMQ, and gRPC
- **Unified Interface**: Same client interface across all transports
- **Publisher/Subscriber**: Real-time messaging support
- **ASP.NET Core Integration**: Seamless integration with existing applications
- **High Performance**: Optimized for different use cases and performance requirements
