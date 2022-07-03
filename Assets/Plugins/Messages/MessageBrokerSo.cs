using System;
using System.Collections.Generic;
using UnityEngine;

namespace Messages
{
    [CreateAssetMenu(fileName = "MessageBroker", menuName = "MessageBroker")]
    public class MessageBrokerSo : ScriptableObject, IMessageBroker
    {
        public object Lock => _lock;
        public Dictionary<Type, Dictionary<Type, Dictionary<object, FreeList<Action<object>>>>> SubscribedActionsKeyed => _subscribedActionsKeyed;
        public Dictionary<Type, FreeList<Action<object>>> SubscribedActions => _subscribedActions;
        
        private readonly Dictionary<Type, FreeList<Action<object>>> _subscribedActions = new();
        private readonly Dictionary<Type, Dictionary<Type, Dictionary<object, FreeList<Action<object>>>>> _subscribedActionsKeyed = new();

        private readonly object _lock = new();
        
        public void Publish<TMessage>(string publisherName = null)
        {
            Publish(Activator.CreateInstance<TMessage>(), publisherName);
        }

        public void Publish<TMessage>(TMessage message, string publisherName = null)
        {
            PublishInner<TMessage>(message, publisherName);
        }

        public void Publish<TKey, TMessage>(TKey key, string publisherName = null)
        {
            Publish(key, Activator.CreateInstance<TMessage>(), publisherName);
        }

        public void Publish<TKey, TMessage>(TKey key, TMessage message, string publisherName = null)
        {
            var keyType = typeof(TKey);
            var type = typeof(TMessage);
            PublishInner(type, keyType, key, message, publisherName);

            var interfaces = type.GetInterfaces();
            for (var i = 0; i < interfaces.Length; i++)
            {
                PublishInner(interfaces[i], keyType, key, message, publisherName);
            }
        }

        private void PublishInner<TMessage>(object message, string publisherName)
        {
            var type = typeof(TMessage);
            lock (_lock)
            {
                Debug.Log($"<color=orange>[OnPublish]:</color> {publisherName} -> <b>{type.Name}</b>");
                if (_subscribedActions.ContainsKey(type)) PublishInner(message, _subscribedActions[type]);

                var interfaces = type.GetInterfaces();
                for (var i = 0; i < interfaces.Length; i++)
                {
                    if (_subscribedActions.ContainsKey(interfaces[i]))
                        PublishInner(message, _subscribedActions[interfaces[i]]);
                }
            }
        }

        private void PublishInner(Type type, Type keyType, object key, object message, string publisherName)
        {
            lock (_lock)
            {
                Debug.Log($"<color=orange>[OnPublish]:</color> {publisherName} -> ({key}) - <b>{type.Name}</b>");
                if (_subscribedActionsKeyed.ContainsKey(type))
                {
                    if (_subscribedActionsKeyed[type].ContainsKey(keyType))
                    {
                        if (_subscribedActionsKeyed[type][keyType].ContainsKey(key))
                        {
                            PublishInner(message, _subscribedActionsKeyed[type][keyType][key]);
                        }
                    }
                }
            }
        }

        private void PublishInner(object message, FreeList<Action<object>> handlers)
        {
            var handlersArray = handlers.GetValues();
            for (var i = 0; i < handlers.GetCount(); i++)
            {
                var handler = handlersArray[i];
                handler?.Invoke(message);
            }
        }

        public IDisposable Subscribe<TMessage>(Action<TMessage> handler)
        {
            lock (_lock)
            {
                var type = typeof(TMessage);
                int subscriptionIndex;

                var action = new Action<object>(o =>
                {
                    Debug.Log($"<color=olive>[OnSubscribe]:</color> <b>{type.Name}</b> -> <color=green>{handler.Method.DeclaringType} > {handler.Method}</color>");
                    handler((TMessage) o);
                });
                if (_subscribedActions.ContainsKey(type))
                {
                    subscriptionIndex = _subscribedActions[type].Add(action);
                }
                else
                {
                    var handlers = new FreeList<Action<object>>();
                    subscriptionIndex = handlers.Add(action);
                    _subscribedActions.Add(type, handlers);
                }

                return new SubscriptionTypeIndex(this, type, subscriptionIndex);
            }
        }

        public IDisposable Subscribe<TKey, TMessage>(TKey key, Action<TMessage> handler)
        {
            lock (_lock)
            {
                var type = typeof(TMessage);
                var keyType = typeof(TKey);
                int subscriptionIndex = 0;

                var action = new Action<object>(o =>
                {
                    Debug.Log($"<color=olive>[OnSubscribe]:</color> ({keyType.Name}) - <b>{type.Name}</b> -> <color=green>{handler.Method.DeclaringType} > {handler.Method}</color>");
                    handler((TMessage) o);
                });
                if (_subscribedActionsKeyed.ContainsKey(type))
                {
                    if (_subscribedActionsKeyed[type].ContainsKey(keyType))
                    {
                        if (_subscribedActionsKeyed[type][keyType].ContainsKey(key))
                        {
                            _subscribedActionsKeyed[type][keyType][key].Add(action);
                        }
                        else
                        {
                            AddHandler();
                        }
                    }
                    else
                    {
                        _subscribedActionsKeyed[type].Add(keyType, new Dictionary<object, FreeList<Action<object>>>());
                        AddHandler();
                    }
                }
                else
                {
                    _subscribedActionsKeyed.Add(type,
                        new Dictionary<Type, Dictionary<object, FreeList<Action<object>>>>());
                    _subscribedActionsKeyed[type].Add(keyType, new Dictionary<object, FreeList<Action<object>>>());
                    AddHandler();
                }

                void AddHandler()
                {
                    var handlers = new FreeList<Action<object>>();
                    subscriptionIndex = handlers.Add(action);
                    _subscribedActionsKeyed[type][keyType].Add(key, handlers);
                }

                return new SubscriptionTypeKeyIndex(this, type, keyType, key, subscriptionIndex);
            }
        }
    }
}