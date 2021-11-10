using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Contrib.RedisStore.Stores
{
  /// <summary>
  /// Provides the implementation of IPersistedGrantStore for Redis Cache.
  /// </summary>
  public class PersistedGrantStore : IPersistedGrantStore
  {
    /// <summary>
    ///
    /// </summary>
    protected readonly RedisOperationalStoreOptions options;

    /// <summary>
    ///
    /// </summary>
    protected readonly IDatabase database;

    /// <summary>
    ///
    /// </summary>
    protected readonly ILogger<PersistedGrantStore> logger;

    /// <summary>
    ///
    /// </summary>
    protected ISystemClock clock;

    /// <summary>
    /// Constructor of class PersistedGrantStore
    /// </summary>
    /// <param name="multiplexer"></param>
    /// <param name="logger"></param>
    /// <param name="clock"></param>
    public PersistedGrantStore(
      RedisMultiplexer<RedisOperationalStoreOptions> multiplexer,
      ILogger<PersistedGrantStore> logger,
      ISystemClock clock)
    {
      if (multiplexer is null)
        throw new ArgumentNullException(nameof(multiplexer));
      this.options = multiplexer.RedisOptions;
      this.database = multiplexer.Database;
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.clock = clock;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected string GetKey(string key) => $"{this.options.KeyPrefix}{key}";

    /// <summary>
    ///
    /// </summary>
    /// <param name="subjectId"></param>
    /// <returns></returns>
    protected string GetSetKey(string subjectId) => $"{this.options.KeyPrefix}{subjectId}";

    /// <summary>
    ///
    /// </summary>
    /// <param name="subjectId"></param>
    /// <param name="clientId"></param>
    /// <returns></returns>
    protected string GetSetKey(string subjectId, string clientId) => $"{this.options.KeyPrefix}{subjectId}:{clientId}";

    /// <summary>
    ///
    /// </summary>
    /// <param name="subjectId"></param>
    /// <param name="clientId"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    protected string GetSetKeyWithType(
      string subjectId,
      string clientId,
      string type) => $"{this.options.KeyPrefix}{subjectId}:{clientId}:{type}";

    /// <summary>
    ///
    /// </summary>
    /// <param name="subjectId"></param>
    /// <param name="clientId"></param>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    protected string GetSetKeyWithSession(string subjectId, string clientId, string sessionId)
    {
      return $"{this.options.KeyPrefix}{subjectId}:{clientId}:{sessionId}";
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="grant"></param>
    /// <returns></returns>
    public virtual async Task StoreAsync(PersistedGrant grant)
    {
      if (grant == null)
        throw new ArgumentNullException(nameof(grant));
      try
      {
        var data = ConvertToJson(grant);
        var grantKey = GetKey(grant.Key);
        var expiresIn = grant.Expiration - this.clock.UtcNow;
        if (!string.IsNullOrEmpty(grant.SubjectId))
        {
          var setKeyforType = GetSetKeyWithType(grant.SubjectId, grant.ClientId, grant.Type);
          var setKeyforSubject = GetSetKey(grant.SubjectId);
          var setKeyforClient = GetSetKey(grant.SubjectId, grant.ClientId);
          var setKetforSession = GetSetKeyWithSession(grant.SubjectId, grant.ClientId, grant.SessionId);

          var ttlOfClientSet = this.database.KeyTimeToLiveAsync(setKeyforClient);
          var ttlOfSubjectSet = this.database.KeyTimeToLiveAsync(setKeyforSubject);
          var ttlofSessionSet = this.database.KeyTimeToLiveAsync(setKetforSession);

          await Task.WhenAll(ttlOfSubjectSet, ttlOfClientSet, ttlofSessionSet);

          var transaction = this.database.CreateTransaction();
          _ = transaction.StringSetAsync(grantKey, data, expiresIn);
          _ = transaction.SetAddAsync(setKeyforSubject, grantKey);
          _ = transaction.SetAddAsync(setKeyforClient, grantKey);
          _ = transaction.SetAddAsync(setKeyforType, grantKey);
          if (!grant.SessionId.IsNullOrEmpty())
            _ = transaction.SetAddAsync(setKetforSession, grantKey);
          if ((ttlOfSubjectSet.Result ?? TimeSpan.Zero) <= expiresIn)
            _ = transaction.KeyExpireAsync(setKeyforSubject, expiresIn);
          if ((ttlOfClientSet.Result ?? TimeSpan.Zero) <= expiresIn)
            _ = transaction.KeyExpireAsync(setKeyforClient, expiresIn);
          if (!grant.SessionId.IsNullOrEmpty() && (ttlofSessionSet.Result ?? TimeSpan.Zero) <= expiresIn)
            _ = transaction.KeyExpireAsync(setKetforSession, expiresIn);
          _ = transaction.KeyExpireAsync(setKeyforType, expiresIn);
          await transaction.ExecuteAsync();
        }
        else
        {
          await this.database.StringSetAsync(grantKey, data, expiresIn);
        }
        logger.LogDebug(
          "grant for subject {subjectId}, clientId {clientId}, grantType {grantType} and " +
          "sessionId {session} persisted successfully",
          grant.SubjectId,
          grant.ClientId,
          grant.Type,
          grant.SessionId);
      }
      catch (Exception ex)
      {
        logger.LogError(
          ex,
          "exception storing persisted grant to Redis database for subject {subjectId}, clientId {clientId}, " +
          "grantType {grantType} and session {sessionId}",
          grant.SubjectId,
          grant.ClientId,
          grant.Type,
          grant.SessionId);
        throw;
      }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual async Task<PersistedGrant> GetAsync(string key)
    {
      try
      {
        var data = await this.database.StringGetAsync(GetKey(key));
        logger.LogDebug("{key} found in database: {hasValue}", key, data.HasValue);
        return data.HasValue ? ConvertFromJson(data) : null;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "exception retrieving grant for key {key}", key);
        throw;
      }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public virtual async Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
    {
      try
      {
        var setKey = GetSetKey(filter);
        var (grants, keysToDelete) = await GetGrants(setKey);
        if (keysToDelete.Any())
        {
          var keys = keysToDelete.ToArray();
          var transaction = this.database.CreateTransaction();
          _ = transaction.SetRemoveAsync(GetSetKey(filter.SubjectId), keys);
          _ = transaction.SetRemoveAsync(GetSetKey(filter.SubjectId, filter.ClientId), keys);
          _ = transaction.SetRemoveAsync(GetSetKeyWithType(filter.SubjectId, filter.ClientId, filter.Type), keys);
          _ = transaction.SetRemoveAsync(GetSetKeyWithSession(filter.SubjectId, filter.ClientId, filter.SessionId), keys);
          await transaction.ExecuteAsync();
        }
        logger.LogDebug("{grantsCount} persisted grants found for {subjectId}", grants.Count(), filter.SubjectId);
        return grants.Where(_ => _.HasValue).Select(_ => ConvertFromJson(_)).Where(_ => IsMatch(_, filter));
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "exception while retrieving grants");
        throw;
      }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="setKey"></param>
    /// <returns></returns>
    protected virtual async Task<(IEnumerable<RedisValue> grants, IEnumerable<RedisValue> keysToDelete)> GetGrants(string setKey)
    {
      var grantsKeys = await this.database.SetMembersAsync(setKey);
      if (!grantsKeys.Any())
        return (Enumerable.Empty<RedisValue>(), Enumerable.Empty<RedisValue>());
      var grants = await this.database.StringGetAsync(grantsKeys.Select(_ => (RedisKey)_.ToString()).ToArray());
      var keysToDelete = grantsKeys.Zip(grants, (key, value) => new KeyValuePair<RedisValue, RedisValue>(key, value))
        .Where(_ => !_.Value.HasValue).Select(_ => _.Key);
      return (grants, keysToDelete);
    }


    /// <summary>
    ///
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual async Task RemoveAsync(string key)
    {
      try
      {
        var grant = await this.GetAsync(key);
        if (grant == null)
        {
          logger.LogDebug("no {key} persisted grant found in database", key);
          return;
        }
        var grantKey = GetKey(key);
        logger.LogDebug("removing {key} persisted grant from database", key);
        var transaction = this.database.CreateTransaction();
        _ = transaction.KeyDeleteAsync(grantKey);
        _ = transaction.SetRemoveAsync(GetSetKey(grant.SubjectId), grantKey);
        _ = transaction.SetRemoveAsync(GetSetKey(grant.SubjectId, grant.ClientId), grantKey);
        _ = transaction.SetRemoveAsync(GetSetKeyWithType(grant.SubjectId, grant.ClientId, grant.Type), grantKey);
        _ = transaction.SetRemoveAsync(GetSetKeyWithSession(grant.SubjectId, grant.ClientId, grant.SessionId), grantKey);
        await transaction.ExecuteAsync();
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "exception removing {key} persisted grant from database", key);
        throw;
      }

    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public virtual async Task RemoveAllAsync(PersistedGrantFilter filter)
    {
      try
      {
        filter.Validate();
        var setKey = GetSetKey(filter);
        var grants = await this.database.SetMembersAsync(setKey);
        logger.LogDebug(
          "removing {grantKeysCount} persisted grants from database for subject {subjectId}, " +
          "clientId {clientId}, grantType {type} and session {session}",
          grants.Count(),
          filter.SubjectId,
          filter.ClientId,
          filter.Type,
          filter.SessionId);
        if (!grants.Any()) return;
        var transaction = this.database.CreateTransaction();
        _ = transaction.KeyDeleteAsync(grants.Select(_ => (RedisKey)_.ToString()).Concat(new RedisKey[] { setKey }).ToArray());
        _ = transaction.SetRemoveAsync(GetSetKey(filter.SubjectId), grants);
        _ = transaction.SetRemoveAsync(GetSetKey(filter.SubjectId, filter.ClientId), grants);
        _ = transaction.SetRemoveAsync(GetSetKeyWithType(filter.SubjectId, filter.ClientId, filter.Type), grants);
        _ = transaction.SetRemoveAsync(GetSetKeyWithSession(filter.SubjectId, filter.ClientId, filter.SessionId), grants);
        await transaction.ExecuteAsync();
      }
      catch (Exception ex)
      {
        logger.LogError(
          ex,
          "exception removing persisted grants from database for subject {subjectId}, clientId {clientId}, " +
          "grantType {type} and session {session}",
          filter.SubjectId,
          filter.ClientId,
          filter.Type,
          filter.SessionId);
        throw;
      }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    protected virtual string GetSetKey(PersistedGrantFilter filter) =>
        (!filter.ClientId.IsNullOrEmpty(), !filter.SessionId.IsNullOrEmpty(), !filter.Type.IsNullOrEmpty()) switch
        {
          (true, true, false) => GetSetKeyWithSession(filter.SubjectId, filter.ClientId, filter.SessionId),
          (true, _, false) => GetSetKey(filter.SubjectId, filter.ClientId),
          (true, _, true) => GetSetKeyWithType(filter.SubjectId, filter.ClientId, filter.Type),
          _ => GetSetKey(filter.SubjectId),
        };

    /// <summary>
    ///
    /// </summary>
    /// <param name="grant"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    protected bool IsMatch(PersistedGrant grant, PersistedGrantFilter filter)
    {
      return (filter.SubjectId.IsNullOrEmpty() || grant.SubjectId == filter.SubjectId)
          && (filter.ClientId.IsNullOrEmpty() || grant.ClientId == filter.ClientId)
          && (filter.SessionId.IsNullOrEmpty() || grant.SessionId == filter.SessionId)
          && (filter.Type.IsNullOrEmpty() || grant.Type == filter.Type);
    }

    #region Json
    /// <summary>
    /// Serialize PersistGrand info to Json
    /// </summary>
    /// <param name="grant"></param>
    /// <returns></returns>
    protected static string ConvertToJson(PersistedGrant grant)
    {
      return JsonConvert.SerializeObject(grant);
    }

    /// <summary>
    /// Deserialize Json to PersistGrand
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    protected static PersistedGrant ConvertFromJson(string data)
    {
      return JsonConvert.DeserializeObject<PersistedGrant>(data);
    }
    #endregion
  }
}
