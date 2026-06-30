using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class InMemoryRateLimiterServiceTests
{
    private InMemoryRateLimiterService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new InMemoryRateLimiterService();
    }

    [TestMethod]
    public async Task GetBucketAsync_ShouldReturnNull_WhenBucketDoesNotExist()
    {
        var bucket = await _service.GetBucketAsync("client1");

        Assert.IsNull(bucket);
    }

    [TestMethod]
    public async Task GetToken_ShouldAddFirstToken()
    {
        var result = await _service.GetToken(
            "client1",
            "token1",
            5,
            TimeSpan.FromMinutes(5));

        var bucket = await _service.GetBucketAsync("client1");

        Assert.IsTrue(result.Allowed);
        Assert.IsNotNull(bucket);
        Assert.AreEqual(1, bucket!.Tokens.Count);
        Assert.AreEqual("token1", bucket.Tokens[0].Token);
    }

    [TestMethod]
    public async Task HasReachedThreshold_ShouldReturnFalse_WhenBelowThreshold()
    {
        await _service.GetToken(
            "client1",
            "token1",
            5,
            TimeSpan.FromMinutes(5));

        var reached = await _service.HasReachedThresholdAsync("client1", 5);

        Assert.IsFalse(reached);
    }

    [TestMethod]
    public async Task HasReachedThreshold_ShouldReturnTrue_WhenThresholdReached()
    {
        for (int i = 0; i < 5; i++)
        {
            await _service.GetToken(
                "client1",
                $"token{i}",
                5,
                TimeSpan.FromMinutes(5));
        }

        var reached = await _service.HasReachedThresholdAsync("client1", 5);

        Assert.IsTrue(reached);
    }

    [TestMethod]
    public async Task GetToken_ShouldRejectRequest_WhenThresholdReached()
    {
        for (int i = 0; i < 5; i++)
        {
            await _service.GetToken(
                "client1",
                $"token{i}",
                5,
                TimeSpan.FromMinutes(5));
        }

        var result = await _service.GetToken(
            "client1",
            "anotherToken",
            5,
            TimeSpan.FromMinutes(5));

        Assert.IsFalse(result.Allowed);
        Assert.AreEqual(0, result.Remaining);
    }

    [TestMethod]
    public async Task RemoveBucket_ShouldRemoveBucket()
    {
        await _service.GetToken(
            "client1",
            "token1",
            5,
            TimeSpan.FromMinutes(5));

        await _service.RemoveBucketAsync("client1");

        var bucket = await _service.GetBucketAsync("client1");

        Assert.IsNull(bucket);
    }

    [TestMethod]
    public async Task ExpiredTokens_ShouldBeRemoved()
    {
        await _service.GetToken(
            "client1",
            "token1",
            5,
            TimeSpan.FromMilliseconds(100));

        await Task.Delay(200);

        var bucket = await _service.GetBucketAsync("client1");

        Assert.IsNull(bucket);
    }

    [TestMethod]
    public async Task DifferentClients_ShouldHaveIndependentBuckets()
    {
        await _service.GetToken(
            "client1",
            "token1",
            5,
            TimeSpan.FromMinutes(5));

        await _service.GetToken(
            "client2",
            "token2",
            5,
            TimeSpan.FromMinutes(5));

        var bucket1 = await _service.GetBucketAsync("client1");
        var bucket2 = await _service.GetBucketAsync("client2");

        Assert.IsNotNull(bucket1);
        Assert.IsNotNull(bucket2);
        Assert.AreEqual(1, bucket1!.Tokens.Count);
        Assert.AreEqual(1, bucket2!.Tokens.Count);
    }
}