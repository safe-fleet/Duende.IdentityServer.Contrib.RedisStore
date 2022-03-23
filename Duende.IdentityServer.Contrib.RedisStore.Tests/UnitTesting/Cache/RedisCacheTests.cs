using System;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityServer.Contrib.RedisStore.Cache;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Duende.IdentityServer.Contrib.RedisStore.Tests.Cache
{
  public class RedisCacheTests
  {
    private readonly RedisCache<string> _cache;

    public RedisCacheTests()
    {
      var logger = new Mock<ILogger<RedisCache<string>>>();
      var options = new RedisCacheOptions { RedisConnectionString = ConfigurationUtils.GetConfiguration()["Redis:ConnectionString"] };
      var multiplexer = new RedisMultiplexer<RedisCacheOptions>(options);

      _cache = new RedisCache<string>(multiplexer, logger.Object);
    }

    [Fact]
    public void RedisCache_Null_Multiplexer_Throws_ArgumentNullException()
    {
      var logger = new Mock<ILogger<RedisCache<string>>>();

      Assert.Throws<ArgumentNullException>(() => new RedisCache<string>(null, logger.Object));
    }

    [Fact]
    public void RedisCache_Null_Logger_Throws_ArgumentNullException()
    {
      var redisCache = new RedisCacheOptions { RedisConnectionString = ConfigurationUtils.GetConfiguration()["Redis:ConnectionString"] };
      var multiplexer = new RedisMultiplexer<RedisCacheOptions>(redisCache);
      Assert.Throws<ArgumentNullException>(() => new RedisCache<string>(multiplexer, null));
    }


    [Fact]
    public async Task SetAsync_Stores_Entries()
    {
      string key = nameof(SetAsync_Stores_Entries);
      string expected = "test_value";
      await _cache.SetAsync(key, expected, TimeSpan.FromSeconds(1));

      var actual = await _cache.GetAsync(key);

      Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetAsync_Does_Not_Return_Expired_Entries()
    {
      string key = nameof(GetAsync_Does_Not_Return_Expired_Entries);
      string expected = "test_value";
      await _cache.SetAsync(key, expected, TimeSpan.FromSeconds(2));

      var actual = await _cache.GetAsync(key);
      Assert.Equal(expected, actual);

      Thread.Sleep(TimeSpan.FromSeconds(2.1));

      actual = await _cache.GetAsync(key);

      Assert.Null(actual);
    }

    [Fact]
    public async Task GetOrAddAsync_Return_Data_Value()
    {
      string key = nameof(GetOrAddAsync_Return_Data_Value);
      string expected = "test_value";
      var actual =  await _cache.GetOrAddAsync(key, TimeSpan.FromSeconds(10),
        () => {
          var s = "test_value";
          return Task.FromResult(s);
      });

      Assert.Equal(expected, actual);

      actual = await _cache.GetOrAddAsync(key, TimeSpan.FromSeconds(10),
        () => {
          var s = "test_value";
          return Task.FromResult(s);
        });

      Assert.Equal(expected, actual);

    }

    [Fact]
    public async Task RemoveAsync()
    {
      string key = nameof(GetOrAddAsync_Return_Data_Value);
      string expected = "test_value";
      var actual = await _cache.GetOrAddAsync(key, TimeSpan.FromSeconds(10),
        () => {
          var s = "test_value";
          return Task.FromResult(s);
        });

      Assert.Equal(expected, actual);

      await _cache.RemoveAsync(key);

      actual = await _cache.GetAsync(key);

      Assert.Null(actual);

    }
  }
}
