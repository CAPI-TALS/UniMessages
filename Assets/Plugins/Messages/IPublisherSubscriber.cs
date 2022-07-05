using System;

namespace Messages
{
    public interface IPublisherSubscriber : IPublisher, ISubscriber { }
    
    public interface IPublisher
    {
        void Publish<TMessage>(string publisherName = null);
        void Publish<TMessage>(TMessage message, string publisherName = null);
        void Publish<TKey, TMessage>(TKey key, string publisherName = null);
        void Publish<TKey, TMessage>(TKey key, TMessage message, string publisherName = null);
    }
    
    public interface ISubscriber
    {
        IDisposable Subscribe<TMessage>(Action<TMessage> handler);
        IDisposable Subscribe<TKey, TMessage>(TKey key, Action<TMessage> handler);
    }
}