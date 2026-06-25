using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Utilities.Attributes;
using Utilities.Enums;
using Utilities.Exceptions;
using Utilities.Extension;
using Utilities.Utilities;

namespace _Utilities.Middlewares
{
    public class CustomRateLimitingMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<CustomRateLimitingMiddleware> logger)
    {
        private static readonly ConcurrentDictionary<string, object> _locks = new();

        public async Task Invoke(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            var rateLimitAttr = endpoint?.Metadata.GetMetadata<CustomRateLimitAttribute>();

            if (rateLimitAttr == null)
            {
                await next(context);
                return;
            }

            // Get endpoint and identifier info
            var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
            var endpointName = $"{actionDescriptor?.ControllerName ?? "Unknown"}.{actionDescriptor?.ActionName ?? "Unknown"}";
            var identifier = await GetIdentifierAsync(context, rateLimitAttr);

            var requestListKey = $"ratelimit_{identifier}_{endpointName}_timestamps";
            var lockKey = $"ratelimit_{identifier}_{endpointName}_lock";

            if (cache.TryGetValue(lockKey, out _))
            {
                context.Response.Headers.RetryAfter = (rateLimitAttr.LockoutDurationMinutes * 60).ToString();

                await context.WriteToResponseAsync(rateLimitAttr.Message, HttpStatusCode.TooManyRequests, ApiResultStatusCode.TooManyRequests);
                return;
            }

            var lockObject = _locks.GetOrAdd(requestListKey, k => new object());

            lock (lockObject)
            {
                var now = DateTime.UtcNow;
                var timestamps = cache.GetOrCreate(requestListKey, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(rateLimitAttr.PeriodSeconds);
                    return new ConcurrentQueue<DateTime>();
                });

                // Remove old timestamps (thread-safe within the lock)
                while (timestamps.TryPeek(out var oldest) && oldest < now.AddSeconds(-rateLimitAttr.PeriodSeconds))
                {
                    timestamps.TryDequeue(out _);
                }

                if (timestamps.Count >= rateLimitAttr.MaxAttemptsCount)
                {
                    cache.Set(lockKey, true, TimeSpan.FromMinutes(rateLimitAttr.LockoutDurationMinutes));
                    cache.Remove(requestListKey);

                    context.Response.Headers.RetryAfter = (rateLimitAttr.LockoutDurationMinutes * 60).ToString();
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                }
                else
                {
                    timestamps.Enqueue(now);

                    var resetTime = new DateTimeOffset(now.AddSeconds(rateLimitAttr.PeriodSeconds)).ToUnixTimeSeconds();
                    context.Response.Headers["X-RateLimit-Limit"] = rateLimitAttr.MaxAttemptsCount.ToString();
                    context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, rateLimitAttr.MaxAttemptsCount - timestamps.Count).ToString();
                    context.Response.Headers["X-RateLimit-Reset"] = resetTime.ToString();
                }
            }

            if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
            {
                throw new TooManyRequestsException(rateLimitAttr.Message);
                //await context.WriteToResponseAsync(rateLimitAttr.Message, HttpStatusCode.TooManyRequests, ApiResultStatusCode.TooManyRequests);
                //return;
            }

            await next(context);
        }

        private async Task<string> GetIdentifierAsync(HttpContext context, CustomRateLimitAttribute rateLimitAttr)
        {
            if (!string.IsNullOrWhiteSpace(rateLimitAttr.RequestBodyKey))
            {
                var bodyKey = await ReadRequestBodyValueAsync(context, rateLimitAttr.RequestBodyKey);

                if (!string.IsNullOrWhiteSpace(bodyKey.Value))
                    return bodyKey.Value.Trim().ToLowerInvariant();

                logger.LogWarning("Rate limit request body key {RequestBodyKey} unavailable: {Reason}",
                    rateLimitAttr.RequestBodyKey, bodyKey.FailureKey);

                return bodyKey.FailureKey;
            }

            var publicKey = context.GetClaim(Claims.PublicKey.ToDisplay());

            if (!string.IsNullOrWhiteSpace(publicKey))
                return publicKey;

            return context.GetRequestIpv4() ?? "unknown";
        }

        private static async Task<(string Value, string FailureKey)> ReadRequestBodyValueAsync(HttpContext context, string key)
        {
            var normalizedKey = key.ToLowerInvariant();

            if (context.Request.Body == null || !context.Request.Body.CanRead)
                return (null, $"body_{normalizedKey}_unreadable");

            context.Request.EnableBuffering();
            if (context.Request.Body.CanSeek)
                context.Request.Body.Position = 0;

            try
            {
                using var document = await JsonDocument.ParseAsync(context.Request.Body);

                if (context.Request.Body.CanSeek)
                    context.Request.Body.Position = 0;

                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    return (null, $"body_{normalizedKey}_invalid");

                var properties = document.RootElement.EnumerateObject()
                    .Where(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (properties.Count == 0)
                    return (null, $"body_{normalizedKey}_missing");

                if (properties.Count > 1)
                    return (null, $"body_{normalizedKey}_duplicate");

                var property = properties[0];
                return property.Value.ValueKind == JsonValueKind.String
                    ? (property.Value.GetString(), null)
                    : (property.Value.ToString(), null);
            }
            catch (JsonException)
            {
                return (null, $"body_{normalizedKey}_malformed");
            }
            finally
            {
                if (context.Request.Body.CanSeek)
                    context.Request.Body.Position = 0;
            }
        }
    }
}
