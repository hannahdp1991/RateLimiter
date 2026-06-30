using RateLimiter.Services;

public interface IRateLimiterService
{
    Task<Bucket?> GetBucketAsync(string clientId);
    Task SetBucketAsync(Bucket bucket);

    Task RemoveBucketAsync(string clientId);

    Task<bool> HasReachedThresholdAsync(string clientId, int threshold);

    Task<RateLimitResult> GetToken(string clientId, string token, int threshold, TimeSpan ttl);
}