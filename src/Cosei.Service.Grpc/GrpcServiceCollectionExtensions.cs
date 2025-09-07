using Cosei.Service.Base;
using Microsoft.Extensions.DependencyInjection;

namespace Cosei.Service.Grpc;

public static class GrpcServiceCollectionExtensions
{
    public static void AddCoseiGrpc(this IServiceCollection services)
    {
        services.AddGrpc();
        services.AddPublisher<GrpcPublisher>();
        services.AddSingleton<CoseiGrpcService>();
    }
}