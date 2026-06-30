using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RateLimiter;
using RateLimiter.Services;
using System.Runtime;

namespace RateLimiter.Controller
{
    /// <summary>
    /// Controller responsible for handling token requests and enforcing rate limiting based on client IP addresses.
    /// </summary>
    [Route("api")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly IRateLimiterService _rateLimiterService;
        private readonly AppSettings _settings;

        public TokenController(IRateLimiterService rateLimiterService, IOptions<AppSettings> options)
        {
            _rateLimiterService = rateLimiterService;
            _settings = options.Value;
        }

        /// <summary>
        /// Gets a token for the client based on their IP address. If the client has exceeded the rate limit, a 429 status code is returned with the retry-after header.
        /// </summary>
        /// <returns>
        /// OK (200) if the token is granted, or Too Many Requests (429) if the rate limit is exceeded.
        /// </returns>
        [HttpGet("GetToken")]
        public async Task<IActionResult> GetToken()
        {
            var clientId = HttpContext.Connection.RemoteIpAddress?.ToString();
            var token = Guid.NewGuid().ToString();
            var result = await _rateLimiterService.GetToken(clientId, token, _settings.Threshold, TimeSpan.FromMinutes(5));

            if (!result.Allowed)
            {
                return RateLimited(result);
            }

            return Allow(result);
        }

        private IActionResult RateLimited(RateLimitResult result)
        {
            Response.Headers["X-Ratelimit-Retry-After"] = result.RetryAfterSeconds.ToString();

            return StatusCode(StatusCodes.Status429TooManyRequests,
                "Rate limit exceeded");
        }

        private IActionResult Allow(RateLimitResult result)
        {
            Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();

            Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();

            Response.Headers["X-RateLimit-Retry-After"] = result.RetryAfterSeconds.ToString();

            return StatusCode(StatusCodes.Status200OK,
                "Received Token");
        }
    }
}
