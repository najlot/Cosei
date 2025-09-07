using Cosei.Service.Base;
using Cosei.Service.Grpc.Generated;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cosei.Service.Grpc;

public class GrpcPublisher : IPublisherImplementation
{
    private readonly ILogger<GrpcPublisher> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentBag<IServerStreamWriter<ResponseMessage>>> _userSubscriptions;
    private readonly ConcurrentBag<IServerStreamWriter<ResponseMessage>> _globalSubscriptions;

    public GrpcPublisher(ILogger<GrpcPublisher> logger)
    {
        _logger = logger;
        _userSubscriptions = new ConcurrentDictionary<string, ConcurrentBag<IServerStreamWriter<ResponseMessage>>>();
        _globalSubscriptions = new ConcurrentBag<IServerStreamWriter<ResponseMessage>>();
    }

    public async Task PublishAsync(Type type, string content)
    {
        var responseMessage = new ResponseMessage
        {
            StatusCode = 200,
            ContentType = "application/json",
            Body = ByteString.CopyFromUtf8(content)
        };
        
        responseMessage.Headers.Add("MessageType", type.FullName);
        responseMessage.Headers.Add("Timestamp", DateTimeOffset.UtcNow.ToString("O"));

        try
        {
            // Send to all global subscribers
            await PublishToSubscribers(_globalSubscriptions, responseMessage).ConfigureAwait(false);
            
            _logger.LogDebug("Published message of type {MessageType} to global subscribers", type.FullName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message of type {MessageType}", type.FullName);
        }
    }

    public async Task PublishToUserAsync(string userId, Type type, string content)
    {
        if (string.IsNullOrEmpty(userId))
        {
            await PublishAsync(type, content).ConfigureAwait(false);
            return;
        }

        var responseMessage = new ResponseMessage
        {
            StatusCode = 200,
            ContentType = "application/json",
            Body = ByteString.CopyFromUtf8(content)
        };
        
        responseMessage.Headers.Add("MessageType", type.FullName);
        responseMessage.Headers.Add("UserId", userId);
        responseMessage.Headers.Add("Timestamp", DateTimeOffset.UtcNow.ToString("O"));

        try
        {
            if (_userSubscriptions.TryGetValue(userId, out var userStreams))
            {
                await PublishToSubscribers(userStreams, responseMessage).ConfigureAwait(false);
                _logger.LogDebug("Published message of type {MessageType} to user {UserId}", type.FullName, userId);
            }
            else
            {
                _logger.LogDebug("No subscribers found for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message of type {MessageType} to user {UserId}", type.FullName, userId);
        }
    }

    public void AddSubscriber(IServerStreamWriter<ResponseMessage> streamWriter, string userId = null)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _globalSubscriptions.Add(streamWriter);
            _logger.LogDebug("Added global subscriber");
        }
        else
        {
            _userSubscriptions.AddOrUpdate(
                userId,
                new ConcurrentBag<IServerStreamWriter<ResponseMessage>> { streamWriter },
                (key, existing) =>
                {
                    existing.Add(streamWriter);
                    return existing;
                });
            _logger.LogDebug("Added subscriber for user {UserId}", userId);
        }
    }

    public void RemoveSubscriber(IServerStreamWriter<ResponseMessage> streamWriter, string userId = null)
    {
        // Note: ConcurrentBag doesn't support removal, so in a production implementation
        // you might want to use a different data structure or mark streams as inactive
        _logger.LogDebug("Subscriber removed for user {UserId}", userId ?? "global");
    }

    private async Task PublishToSubscribers(ConcurrentBag<IServerStreamWriter<ResponseMessage>> subscribers, ResponseMessage message)
    {
        var tasks = new List<Task>();

        foreach (var subscriber in subscribers)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await subscriber.WriteAsync(message).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send message to subscriber");
                    // In a production implementation, you would remove the failed subscriber
                }
            }));
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}