using System;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using System.Linq;
using System.Diagnostics;

namespace Duende.IdentityServer.Contrib.RedisStore
{
  ///<summary>
  /// Represents the Profile Service caching options.
  ///</summary>
  public class ProfileServiceCachingOptions<T> where T : class, IProfileService
  {
    ///<summary>
    /// Key selector for IsActiveContext, defaults select the Subject (sub) claim value.
    /// </summary>
    public Func<IsActiveContext, string> KeySelector { get; set; } = (context) => context.Subject.Claims.First(_ => _.Type == "sub").Value;

    ///<summary>
    /// A predicate to determine whether the current IsActiveContext should be cached or not, default to true on all IsActiveContext instances.
    /// </summary>
    public Func<IsActiveContext, bool> ShouldCache { get; set; } = (context) => true;

    ///<summary>
    /// Expiration of the cache entry of IsActiveContext, defaults to 10 minutes.
    /// </summary>
    public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(10);

    private string _keyPrefix = string.Empty;

    /// <summary>
    /// The Prefix to add to each key stored on Redis Cache, default is Empty.
    /// </summary>
    public string KeyPrefix
    {
      get
      {
        return string.IsNullOrEmpty(this._keyPrefix) ? this._keyPrefix : $"{_keyPrefix}:";
      }
      set
      {
        this._keyPrefix = value;
      }
    }
  }
}
