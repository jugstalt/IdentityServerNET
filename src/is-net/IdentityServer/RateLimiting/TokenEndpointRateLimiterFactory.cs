using Microsoft.AspNetCore.Http;
using System;
using System.Threading.RateLimiting;

namespace IdentityServer.RateLimiting;

// Extracted out of Program.cs purely so the partitioning decision (which path gets limited, keyed by
// which partition) is unit-testable without booting the whole host.
public static class TokenEndpointRateLimiterFactory
{
    public static PartitionedRateLimiter<HttpContext> Create(PathString tokenEndpointPath, int permitLimit, TimeSpan window)
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            if (!httpContext.Request.Path.StartsWithSegments(tokenEndpointPath))
            {
                return RateLimitPartition.GetNoLimiter("unrestricted");
            }

            var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                SegmentsPerWindow = 4,
                QueueLimit = 0
            });
        });
    }
}
