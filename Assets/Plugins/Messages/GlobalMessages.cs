using System;
using System.Collections.Generic;
using UnityEngine;

namespace Messages
{
    public static class GlobalMessages
    {
        private static MessageBroker Instance => _instance ??= new MessageBroker();
        private static MessageBroker _instance;

        public static void Publish<TMessage>(string publisherName = null)
        {
            Instance.Publish<TMessage>(publisherName);
        }

        public static void Publish<TMessage>(TMessage message, string publisherName = null)
        {
            Instance.Publish(message, publisherName);
        }

        public static void Publish<TKey, TMessage>(TKey key, string publisherName = null)
        {
            Instance.Publish<TKey, TMessage>(key, publisherName);
        }

        public static void Publish<TKey, TMessage>(TKey key, TMessage message, string publisherName = null)
        {
            Instance.Publish(key, message, publisherName);
        }

        public static IDisposable Subscribe<TMessage>(Action<TMessage> handler)
        {
            return Instance.Subscribe(handler);
        }

        public static IDisposable Subscribe<TKey, TMessage>(TKey key, Action<TMessage> handler)
        {
            return Instance.Subscribe(key, handler);
        }
    }
}