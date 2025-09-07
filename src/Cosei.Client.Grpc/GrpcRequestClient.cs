using Cosei.Client.Base;
using Cosei.Client.Grpc.Generated;
using Google.Protobuf;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cosei.Client.Grpc;

public class GrpcRequestClient : IRequestClient
{
    private readonly GrpcChannel _channel;
    private readonly CoseiService.CoseiServiceClient _client;

    public GrpcRequestClient(string serverAddress)
    {
        _channel = GrpcChannel.ForAddress(serverAddress);
        _client = new CoseiService.CoseiServiceClient(_channel);
    }

    public GrpcRequestClient(GrpcChannel channel)
    {
        _channel = channel;
        _client = new CoseiService.CoseiServiceClient(_channel);
    }

    public async Task<Response> GetAsync(string requestUri, Dictionary<string, string> headers = null)
    {
        return await ProcessRequestAsync("GET", requestUri, null, null, headers).ConfigureAwait(false);
    }

    public async Task<Response> PostAsync(string requestUri, string request, string contentType, Dictionary<string, string> headers = null)
    {
        return await ProcessRequestAsync("POST", requestUri, request, contentType, headers).ConfigureAwait(false);
    }

    public async Task<Response> PutAsync(string requestUri, string request, string contentType, Dictionary<string, string> headers = null)
    {
        return await ProcessRequestAsync("PUT", requestUri, request, contentType, headers).ConfigureAwait(false);
    }

    public async Task<Response> DeleteAsync(string requestUri, Dictionary<string, string> headers = null)
    {
        return await ProcessRequestAsync("DELETE", requestUri, null, null, headers).ConfigureAwait(false);
    }

    private async Task<Response> ProcessRequestAsync(
        string method, 
        string requestUri, 
        string requestBody, 
        string contentType, 
        Dictionary<string, string> headers)
    {
        var requestMessage = new RequestMessage
        {
            Method = method,
            RequestUri = requestUri,
            ContentType = contentType ?? string.Empty
        };

        if (requestBody != null)
        {
            requestMessage.Body = ByteString.CopyFromUtf8(requestBody);
        }

        if (headers != null)
        {
            foreach (var header in headers)
            {
                requestMessage.Headers.Add(header.Key, header.Value);
            }
        }

        try
        {
            var response = await _client.ProcessRequestAsync(requestMessage).ConfigureAwait(false);
            
            return new Response(
                response.StatusCode,
                response.ContentType,
                response.Body.Memory
            );
        }
        catch (Exception ex)
        {
            // Convert gRPC exceptions to appropriate HTTP exceptions
            throw new System.Net.Http.HttpRequestException($"gRPC request failed: {ex.Message}", ex);
        }
    }

    private bool _disposedValue = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            _disposedValue = true;

            if (disposing)
            {
                _channel?.Dispose();
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}