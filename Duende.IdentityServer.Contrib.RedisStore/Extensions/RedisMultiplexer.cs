using StackExchange.Redis;

namespace Duende.IdentityServer.Contrib.RedisStore
{
  /// <summary>
  /// represents Redis general multiplexer
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class RedisMultiplexer<T> where T : RedisOptions
  {
    /// <summary>
    /// Get data from Redis DataBase
    /// </summary>
    /// <param name="redisOptions"></param>
    public RedisMultiplexer(T redisOptions)
    {
      this.RedisOptions = redisOptions;
      this.GetDatabase();
    }

    private void GetDatabase()
    {
      var redisOption = string.IsNullOrEmpty(RedisOptions.RedisConnectionString) ? -1 : this.RedisOptions.Db;
      this.Database = this.RedisOptions.Multiplexer.GetDatabase(redisOption);
    }

    internal T RedisOptions { get; }

    internal IDatabase Database { get; private set; }
  }
}
