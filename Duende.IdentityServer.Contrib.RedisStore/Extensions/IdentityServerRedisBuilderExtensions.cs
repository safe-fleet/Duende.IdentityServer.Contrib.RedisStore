using Duende.IdentityServer.Contrib.RedisStore;
using Duende.IdentityServer.Contrib.RedisStore.Cache;
using Duende.IdentityServer.Contrib.RedisStore.Stores;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection
{
  /// <summary>
  /// Extension of IdentityServerRedisBuilder
  /// </summary>
  public static class IdentityServerRedisBuilderExtensions
  {
    /// <summary>
    /// Add Redis Operational Store.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="optionsBuilder">Redis Operational Store Options builder</param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddOperationalStore(
      [NotNull] this IIdentityServerBuilder builder,
      [NotNull] Action<RedisOperationalStoreOptions> optionsBuilder)
    {
      if (builder is null) throw new ArgumentNullException(nameof(builder));
      var options = new RedisOperationalStoreOptions();
      optionsBuilder?.Invoke(options);
      builder.Services.AddSingleton(options);

      builder.Services.AddScoped<RedisMultiplexer<RedisOperationalStoreOptions>>();
      builder.Services.AddTransient<IPersistedGrantStore, PersistedGrantStore>();
      return builder;
    }



    /// <summary>
    /// Add Redis caching that implements ICache
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="optionsBuilder">Redis Cache Options builder</param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddRedisCaching(
      [NotNull]  this IIdentityServerBuilder builder,
      [NotNull]  Action<RedisCacheOptions> optionsBuilder)
    {
      if (builder is null) throw new ArgumentNullException(nameof(builder));

      var options = new RedisCacheOptions();
      optionsBuilder?.Invoke(options);
      builder.Services.AddSingleton(options);

      builder.Services.AddScoped<RedisMultiplexer<RedisCacheOptions>>();
      builder.Services.AddTransient(typeof(ICache<>), typeof(RedisCache<>));
      return builder;
    }

    ///<summary>
    /// Add Redis caching for IProfileService Implementation
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="optionsBuilder">Profile Service Redis Cache Options builder</param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddProfileServiceCache<TProfileService>(
      [NotNull] this IIdentityServerBuilder builder,
      Action<ProfileServiceCachingOptions<TProfileService>> optionsBuilder = null)
    where TProfileService : class, IProfileService
    {
      if (builder is null) throw new ArgumentNullException(nameof(builder));

      var options = new ProfileServiceCachingOptions<TProfileService>();
      optionsBuilder?.Invoke(options);
      builder.Services.AddSingleton(options);

      builder.Services.TryAddTransient(typeof(TProfileService));
      builder.Services.AddTransient<IProfileService, CachingProfileService<TProfileService>>();
      return builder;
    }
  }
}
