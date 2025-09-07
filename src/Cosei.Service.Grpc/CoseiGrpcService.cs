using Cosei.Service.Base;
using Cosei.Service.Grpc.Generated;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cosei.Service.Grpc;

public class CoseiGrpcService : CoseiService.CoseiServiceBase
{
    private readonly IRequestDelegateProvider _requestDelegateProvider;
    private readonly ILogger<CoseiGrpcService> _logger;

    public CoseiGrpcService(IRequestDelegateProvider requestDelegateProvider, ILogger<CoseiGrpcService> logger)
    {
        _requestDelegateProvider = requestDelegateProvider;
        _logger = logger;
    }

    public override async Task<ResponseMessage> ProcessRequest(RequestMessage request, ServerCallContext context)
    {
        try
        {
            // Create a HttpContext-like environment for the request
            var httpContext = CreateHttpContext(request, context);
            
            // Get the appropriate request delegate from the provider
            var requestDelegate = _requestDelegateProvider.RequestDelegate;
            
            // Execute the request delegate
            await requestDelegate(httpContext).ConfigureAwait(false);
            
            // Convert the response back to gRPC response
            return await CreateResponseMessage(httpContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing gRPC request: {Method} {RequestUri}", request.Method, request.RequestUri);
            
            return new ResponseMessage
            {
                StatusCode = 500,
                ContentType = "text/plain",
                Body = ByteString.CopyFromUtf8($"Internal Server Error: {ex.Message}")
            };
        }
    }

    public override async Task Subscribe(SubscriptionRequest request, IServerStreamWriter<ResponseMessage> responseStream, ServerCallContext context)
    {
        // This would be used for server-side streaming for publisher scenarios
        // For now, we'll implement a basic streaming response
        // In a real implementation, this would integrate with the publisher system
        
        try
        {
            // Keep the stream alive and wait for cancellation
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, context.CancellationToken);
                // In a real implementation, this would stream actual published messages
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when client disconnects
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC subscription for user: {UserId}", request.UserId);
        }
    }

    private HttpContext CreateHttpContext(RequestMessage request, ServerCallContext context)
    {
        // Create a minimal HttpContext for processing the request
        var httpContext = new DefaultHttpContext();
        
        // Set the request method and path
        httpContext.Request.Method = request.Method;
        httpContext.Request.Path = request.RequestUri;
        
        // Set content type if provided
        if (!string.IsNullOrEmpty(request.ContentType))
        {
            httpContext.Request.ContentType = request.ContentType;
        }
        
        // Add headers
        foreach (var header in request.Headers)
        {
            httpContext.Request.Headers[header.Key] = header.Value;
        }
        
        // Set request body if provided
        if (request.Body != null && !request.Body.IsEmpty)
        {
            var bodyStream = new MemoryStream(request.Body.ToByteArray());
            httpContext.Request.Body = bodyStream;
            httpContext.Request.ContentLength = bodyStream.Length;
        }
        
        // Prepare response stream
        httpContext.Response.Body = new MemoryStream();
        
        return httpContext;
    }

    private Task<ResponseMessage> CreateResponseMessage(HttpContext httpContext)
    {
        var response = new ResponseMessage
        {
            StatusCode = httpContext.Response.StatusCode,
            ContentType = httpContext.Response.ContentType ?? "application/octet-stream"
        };
        
        // Read response body
        if (httpContext.Response.Body is MemoryStream responseStream)
        {
            responseStream.Position = 0;
            var responseBytes = responseStream.ToArray();
            response.Body = ByteString.CopyFrom(responseBytes);
        }
        
        // Add response headers
        foreach (var header in httpContext.Response.Headers)
        {
            response.Headers.Add(header.Key, string.Join(", ", header.Value));
        }
        
        return Task.FromResult(response);
    }
}