using System;

namespace Messages
{
    public interface ISubscriber
    {
        IDisposable Subscribe<TMessage>(Action<TMessage> handler);
        IDisposable Subscribe<TKey, TMessage>(TKey key, Action<TMessage> handler);
    }
}