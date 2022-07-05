using System;

namespace Messages
{
    public static class GlobalMessages
    {
        public static IPublisherSubscriber MessageBroker => _instance ??= new MessageBroker();
        private static MessageBroker _instance;

        public static void Publish<TMessage>(string publisherName = null)
        {
            MessageBroker.Publish<TMessage>(publisherName);
        }

        public static void Publish<TMessage>(TMessage message, string publisherName = null)
        {
            MessageBroker.Publish(message, publisherName);
        }

        public static void Publish<TKey, TMessage>(TKey key, string publisherName = null)
        {
            MessageBroker.Publish<TKey, TMessage>(key, publisherName);
        }

        public static void Publish<TKey, TMessage>(TKey key, TMessage message, string publisherName = null)
        {
            MessageBroker.Publish(key, message, publisherName);
        }

        public static IDisposable Subscribe<TMessage>(Action<TMessage> handler)
        {
            return MessageBroker.Subscribe(handler);
        }

        public static IDisposable Subscribe<TKey, TMessage>(TKey key, Action<TMessage> handler)
        {
            return MessageBroker.Subscribe(key, handler);
        }
    }
}