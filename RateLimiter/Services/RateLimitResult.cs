namespace RateLimiter.Services
{
    public class RateLimitResult
    {
        public bool Allowed { get; init; }

        public int Limit { get; init; }

        public int Remaining { get; init; }

        public int RetryAfterSeconds { get; init; }
    }
}
