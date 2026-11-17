using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace GZCTF.Middlewares;

/// <summary>
/// The rate limiter middleware
/// </summary>
[ExcludeFromCodeCoverage]
public static class RateLimiter
{
    public enum LimitPolicy
    {
        /// <summary>
        /// Concurrency operation limit
        /// </summary>
        Concurrency,

        /// <summary>
        /// Register limit
        /// </summary>
        Register,

        /// <summary>
        /// Database query limit
        /// </summary>
        Query,

        /// <summary>
        /// Container operation limit
        /// </summary>
        Container,

        /// <summary>
        /// Flag submit limit
        /// </summary>
        Submit,

        /// <summary>
        /// Pow challenge generation limit
        /// </summary>
        PowChallenge
    }

    public static void ConfigureRateLimiter(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (context.User.Identity?.IsAuthenticated is true && userId is not null)
                return RateLimitPartition.GetSlidingWindowLimiter(GetPartitionKey(context),
                    _ => new()
                    {
                        QueueLimit = 60,
                        PermitLimit = 150,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        SegmentsPerWindow = 6
                    });

            var address = context.Connection.RemoteIpAddress;

            if (address is null || IPAddress.IsLoopback(address))
                return RateLimitPartition.GetNoLimiter(IPAddress.Loopback.ToString());

            return RateLimitPartition.GetSlidingWindowLimiter(GetPartitionKey(context),
                _ => new()
                {
                    QueueLimit = 60,
                    PermitLimit = 150,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    SegmentsPerWindow = 6
                });
        });
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = MediaTypeNames.Application.Json;

            var localizer =
                context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<Program>>();
            var afterSec = (int)TimeSpan.FromMinutes(1).TotalSeconds;

            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                afterSec = (int)retryAfter.TotalSeconds;

            context.HttpContext.Response.Headers.RetryAfter = afterSec.ToString(NumberFormatInfo.InvariantInfo);
            await context.HttpContext.Response.WriteAsJsonAsync(
                new RequestResponse(localizer[nameof(Resources.Program.RateLimit_TooManyRequests), afterSec],
                    StatusCodes.Status429TooManyRequests
                ), cancellationToken);
        };
        options.AddPolicy(nameof(LimitPolicy.Concurrency), context =>
            RateLimitPartition.GetConcurrencyLimiter(GetPartitionKey(context), _ => new()
            {
                PermitLimit = 1,
                QueueLimit = 20
            }));
        options.AddPolicy(nameof(LimitPolicy.Register), context =>
            RateLimitPartition.GetSlidingWindowLimiter(GetPartitionKey(context), _ => new()
            {
                QueueLimit = 10,
                PermitLimit = 20,
                Window = TimeSpan.FromSeconds(150),
                QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
                SegmentsPerWindow = 5
            }));
        options.AddPolicy(nameof(LimitPolicy.Query), context =>
            RateLimitPartition.GetTokenBucketLimiter(GetPartitionKey(context), _ => new()
            {
                TokenLimit = 100,
                TokensPerPeriod = 10,
                ReplenishmentPeriod = TimeSpan.FromSeconds(10)
            }));
        options.AddPolicy(nameof(LimitPolicy.PowChallenge), context =>
            RateLimitPartition.GetTokenBucketLimiter(GetPartitionKey(context), _ => new()
            {
                TokenLimit = 40,
                TokensPerPeriod = 5,
                ReplenishmentPeriod = TimeSpan.FromSeconds(30)
            }));
        options.AddPolicy(nameof(LimitPolicy.Container), context =>
            RateLimitPartition.GetTokenBucketLimiter(GetPartitionKey(context), _ => new()
            {
                TokenLimit = 120,
                TokensPerPeriod = 30,
                ReplenishmentPeriod = TimeSpan.FromSeconds(10)
            }));
        options.AddPolicy(nameof(LimitPolicy.Submit), context =>
            RateLimitPartition.GetTokenBucketLimiter(GetPartitionKey(context), _ => new()
            {
                TokenLimit = 100,
                TokensPerPeriod = 50,
                ReplenishmentPeriod = TimeSpan.FromSeconds(5)
            }));
    }

    private static string GetPartitionKey(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated is true &&
            context.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } userId)
            return $"user:{userId}";

        var address = context.Connection.RemoteIpAddress;
        if (address?.IsIPv4MappedToIPv6 is true)
            address = address.MapToIPv4();

        return $"ip:{address}";
    }
}
