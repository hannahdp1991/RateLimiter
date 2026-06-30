using RateLimiter.Services;
using System.Collections.Concurrent;

/// <summary>
/// A Thread-safe, in-memory implementation of the <see cref="IRateLimiterService"/> interface.
/// </summary>
public class InMemoryRateLimiterService : IRateLimiterService
{
    private readonly ConcurrentDictionary<string, Bucket> _store = new();

    /// <summary>
    /// Gets the bucket for the specified client ID. If the bucket does not exist or has expired tokens, it returns null.
    /// </summary>
    /// <param name="clientId">Unique identifier of the client.</param>
    /// <returns> 
    /// The client's bucket if it exists and contains active tokens;
    /// otherwise <c>null</c>.
    /// </returns>
    public Task<Bucket?> GetBucketAsync(string clientId)
    {
        if (!_store.TryGetValue(clientId, out var bucket))
            return Task.FromResult<Bucket?>(null);

        CleanupExpiredTokens(bucket);

        if (bucket.Tokens.Count == 0)
        {
            _store.TryRemove(clientId, out _);
            return Task.FromResult<Bucket?>(null);
        }

        return Task.FromResult<Bucket?>(bucket);
    }

    /// <summary>
    /// Stores or updates the specified bucket after removing expired tokens.
    /// </summary>
    /// <param name="bucket">The bucket to store.</param>
    public Task SetBucketAsync(Bucket bucket)
    {
        CleanupExpiredTokens(bucket);
        _store[bucket.ClientId] = bucket;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes the bucket associated with the specified client.
    /// </summary>
    /// <param name="clientId">Unique identifier of the client.</param>
    public Task RemoveBucketAsync(string clientId)
    {
        _store.TryRemove(clientId, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Determines whether the client has reached the configured request threshold.
    /// </summary>
    /// <param name="clientId">Unique identifier of the client.</param>
    /// <param name="threshold">Maximum number of active requests allowed.</param>
    /// <returns>
    /// <c>true</c> if the client has reached or exceeded the threshold;
    /// otherwise <c>false</c>.
    /// </returns>
    public Task<bool> HasReachedThresholdAsync(string clientId, int threshold)
    {
        if (!_store.TryGetValue(clientId, out var bucket))
            return Task.FromResult(false);

        CleanupExpiredTokens(bucket);

        return Task.FromResult(bucket.Tokens.Count >= threshold);
    }

    /// <summary>
    /// Attempts to register a new request token for the specified client.
    /// </summary>
    /// <param name="clientId">Unique identifier of the client.</param>
    /// <param name="token">Unique token representing the request.</param>
    /// <param name="threshold">Maximum number of active tokens allowed.</param>
    /// <param name="ttl">The lifetime of the token before it expires.</param>
    /// <returns>
    /// A <see cref="RateLimitResult"/> containing the outcome of the request,
    /// including whether it was allowed and the remaining quota.
    /// </returns>
    public async Task<RateLimitResult> GetToken(string clientId, string token, int threshold, TimeSpan ttl)
    {
        var bucket = await GetBucketAsync(clientId)
                     ?? new Bucket { ClientId = clientId };

        CleanupExpiredTokens(bucket);

        if (bucket.Tokens.Count >= threshold)
            return new RateLimitResult
            {
                Allowed = false,
                Limit = threshold,
                Remaining = 0,
                RetryAfterSeconds = 300
            }; ;

        bucket.Tokens.Add(new BucketToken
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.Add(ttl)
        });

        await SetBucketAsync(bucket);

        return new RateLimitResult
        {
            Allowed = true,
            Limit = threshold,
            Remaining = threshold - bucket.Tokens.Count,
            RetryAfterSeconds = 0
        }; ;
    }

    private static void CleanupExpiredTokens(Bucket bucket)
    {
        bucket.Tokens.RemoveAll(t => t.ExpiresAt <= DateTime.UtcNow);
    }
}