using Cosei.Service.Base;
using Cosei.Service.Grpc.Generated;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cosei.Service.Grpc;

public class GrpcPublisher : IPublisherImplementation
{
    private readonly ILogger<GrpcPublisher> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, IServerStreamWriter<ResponseMessage>>> _userSubscriptions;
    private readonly ConcurrentDictionary<Guid, IServerStreamWriter<ResponseMessage>> _globalSubscriptions;

    public GrpcPublisher(ILogger<GrpcPublisher> logger)
    {
        _logger = logger;
        _userSubscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<Guid, IServerStreamWriter<ResponseMessage>>>();
        _globalSubscriptions = new ConcurrentDictionary<Guid, IServerStreamWriter<ResponseMessage>>();
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
            
            _logger.LogDebug("Published message of type {MessageType} to {SubscriberCount} global subscribers", 
                type.FullName, _globalSubscriptions.Count);
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
            if (_userSubscriptions.TryGetValue(userId, out var userStreams) && !userStreams.IsEmpty)
            {
                await PublishToSubscribers(userStreams, responseMessage).ConfigureAwait(false);
                _logger.LogDebug("Published message of type {MessageType} to {SubscriberCount} subscribers for user {UserId}", 
                    type.FullName, userStreams.Count, userId);
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

    public Guid AddSubscriber(IServerStreamWriter<ResponseMessage> streamWriter, string userId = null)
    {
        var subscriptionId = Guid.NewGuid();
        
        if (string.IsNullOrEmpty(userId))
        {
            _globalSubscriptions.TryAdd(subscriptionId, streamWriter);
            _logger.LogDebug("Added global subscriber with ID {SubscriptionId}", subscriptionId);
        }
        else
        {
            _userSubscriptions.AddOrUpdate(
                userId,
                new ConcurrentDictionary<Guid, IServerStreamWriter<ResponseMessage>>(),
                (key, existing) => existing);
                
            if (_userSubscriptions.TryGetValue(userId, out var userStreams))
            {
                userStreams.TryAdd(subscriptionId, streamWriter);
                _logger.LogDebug("Added subscriber with ID {SubscriptionId} for user {UserId}", subscriptionId, userId);
            }
        }
        
        return subscriptionId;
    }

    public bool RemoveSubscriber(Guid subscriptionId, string userId = null)
    {
        bool removed = false;
        
        if (string.IsNullOrEmpty(userId))
        {
            removed = _globalSubscriptions.TryRemove(subscriptionId, out _);
            _logger.LogDebug("Removed global subscriber with ID {SubscriptionId}: {Success}", subscriptionId, removed);
        }
        else
        {
            if (_userSubscriptions.TryGetValue(userId, out var userStreams))
            {
                removed = userStreams.TryRemove(subscriptionId, out _);
                
                // Clean up empty user subscription collections
                if (userStreams.IsEmpty)
                {
                    _userSubscriptions.TryRemove(userId, out _);
                }
                
                _logger.LogDebug("Removed subscriber with ID {SubscriptionId} for user {UserId}: {Success}", 
                    subscriptionId, userId, removed);
            }
        }
        
        return removed;
    }

    private async Task PublishToSubscribers(ConcurrentDictionary<Guid, IServerStreamWriter<ResponseMessage>> subscribers, ResponseMessage message)
    {
        var tasks = new List<Task>();
        var failedSubscribers = new List<Guid>();

        foreach (var kvp in subscribers)
        {
            var subscriptionId = kvp.Key;
            var subscriber = kvp.Value;
            
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await subscriber.WriteAsync(message).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send message to subscriber {SubscriptionId}", subscriptionId);
                    // Mark this subscriber for removal
                    lock (failedSubscribers)
                    {
                        failedSubscribers.Add(subscriptionId);
                    }
                }
            }));
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
            
            // Remove failed subscribers
            foreach (var failedId in failedSubscribers)
            {
                subscribers.TryRemove(failedId, out _);
                _logger.LogDebug("Automatically removed failed subscriber {SubscriptionId}", failedId);
            }
        }
    }
}