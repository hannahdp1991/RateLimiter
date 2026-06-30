namespace RateLimiter.Services
{
    public class Bucket
    {
        public string ClientId { get; set; } = string.Empty;
        public List<BucketToken> Tokens { get; set; } = new();
    }
}
