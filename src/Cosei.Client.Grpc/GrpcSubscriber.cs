using Cosei.Client.Base;
using Cosei.Client.Grpc.Generated;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cosei.Client.Grpc;

public class GrpcSubscriber : AbstractSubscriber
{
    private readonly GrpcChannel _channel;
    private readonly CoseiService.CoseiServiceClient _client;
    private readonly string _userId;
    private readonly List<string> _messageTypes;

    public GrpcSubscriber(string serverAddress, string userId = null, List<string> messageTypes = null)
    {
        _channel = GrpcChannel.ForAddress(serverAddress);
        _client = new CoseiService.CoseiServiceClient(_channel);
        _userId = userId;
        _messageTypes = messageTypes ?? new List<string>();
    }

    public GrpcSubscriber(GrpcChannel channel, string userId = null, List<string> messageTypes = null)
    {
        _channel = channel;
        _client = new CoseiService.CoseiServiceClient(_channel);
        _userId = userId;
        _messageTypes = messageTypes ?? new List<string>();
    }

    public override async Task StartAsync()
    {
        await StartAsync(CancellationToken.None).ConfigureAwait(false);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var subscriptionRequest = new SubscriptionRequest();
        
        if (!string.IsNullOrEmpty(_userId))
        {
            subscriptionRequest.UserId = _userId;
        }

        foreach (var messageType in _messageTypes)
        {
            subscriptionRequest.MessageTypes.Add(messageType);
        }

        try
        {
            using var call = _client.Subscribe(subscriptionRequest, cancellationToken: cancellationToken);
            
            while (await call.ResponseStream.MoveNext(cancellationToken))
            {
                var response = call.ResponseStream.Current;
                // Convert the gRPC response to the expected message type and send it
                var messageTypeHeader = response.Headers.TryGetValue("MessageType", out var messageType) ? messageType : null;
                
                if (!string.IsNullOrEmpty(messageTypeHeader))
                {
                    // Try to deserialize the message based on the type
                    var type = Type.GetType(messageTypeHeader);
                    if (type != null)
                    {
                        var jsonContent = response.Body.ToStringUtf8();
                        var message = System.Text.Json.JsonSerializer.Deserialize(jsonContent, type);
                        
                        // Use reflection to call SendAsync<T> with the correct type
                        var sendMethod = typeof(AbstractSubscriber)
                            .GetMethod("SendAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.MakeGenericMethod(type);
                        
                        if (sendMethod != null && message != null)
                        {
                            await (Task)sendMethod.Invoke(this, new object[] { message });
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation token is triggered
        }
        catch (Exception ex)
        {
            // Log error or handle appropriately
            throw new InvalidOperationException($"gRPC subscription failed: {ex.Message}", ex);
        }
    }

    private bool _disposedValue = false;

    protected override void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            _disposedValue = true;

            if (disposing)
            {
                _channel?.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}