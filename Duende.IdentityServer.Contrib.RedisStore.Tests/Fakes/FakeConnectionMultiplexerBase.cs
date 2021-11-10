using StackExchange.Redis;
using System;

namespace Duende.IdentityServer.Contrib.RedisStore.Tests.Fakes
{
  internal class FakeConnectionMultiplexerBase
  {
#pragma warning disable 67
    public event EventHandler<RedisErrorEventArgs> ErrorMessage;
    public event EventHandler<ConnectionFailedEventArgs> ConnectionFailed;
    public event EventHandler<InternalErrorEventArgs> InternalError;
    public event EventHandler<ConnectionFailedEventArgs> ConnectionRestored;
    public event EventHandler<EndPointEventArgs> ConfigurationChanged;
    public event EventHandler<EndPointEventArgs> ConfigurationChangedBroadcast;
    public event EventHandler<HashSlotMovedEventArgs> HashSlotMoved;
  }
}
