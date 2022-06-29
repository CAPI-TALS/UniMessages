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
            Publish(typeof(TMessage), Activator.CreateInstance<TMessage>(), publisherName);
        }

        public static void Publish<TMessage>(TMessage message, string publisherName = null)
        {
            Instance.Publish(typeof(TMessage), message, publisherName);
        }

        public static void Publish<TKey, TMessage>(TKey key, TMessage message, string publisherName = null)
        {
            var keyType = typeof(TKey);
            var type = typeof(TMessage);
            Instance.Publish(type, keyType, key, message, publisherName);

            var interfaces = type.GetInterfaces();
            for (var i = 0; i < interfaces.Length; i++)
            {
                Instance.Publish(interfaces[i], keyType, key, message, publisherName);
            }
        }

        public static TResponse Publish<TMessage, TResponse>(string publisherName = null)
        {
            return Publish<TMessage, TResponse>(Activator.CreateInstance<TMessage>(), publisherName);
        }

        public static TResponse Publish<TMessage, TResponse>(TMessage message, string publisherName = null)
        {
            return Instance.Publish<TMessage, TResponse>(message, publisherName);
        }

        public static IDisposable Subscribe<TMessage>(Action<TMessage> handler)
        {
            return Instance.Subscribe(handler);
        }

        public static IDisposable Subscribe<TMessage, TResponse>(Func<TMessage, TResponse> handler)
        {
            return Instance.Subscribe(handler);
        }

        public static IDisposable Subscribe<TKey, TMessage>(TKey key, Action<TMessage> handler)
        {
            return Instance.Subscribe(key, handler);
        }

        private class MessageBroker
        {
            private readonly Dictionary<Type, FreeList<Action<object>>> _subscribedActions = new();
            private readonly Dictionary<Type, Dictionary<Type, Dictionary<object, FreeList<Action<object>>>>> _subscribedActionsKeyed = new();
            private readonly Dictionary<Type, object> _subscribedFunctions = new();
            private readonly object _lock = new();

            public void Publish(Type type, object message, string publisherName)
            {
                lock (_lock)
                {
                    Debug.Log($"<color=orange>[OnPublish]:</color> {publisherName} -> <b>{type.Name}</b>");
                    if (_subscribedActions.ContainsKey(type)) Publish(message, _subscribedActions[type]);
                    
                    var interfaces = type.GetInterfaces();
                    for (var i = 0; i < interfaces.Length; i++)
                    {
                        if (_subscribedActions.ContainsKey(interfaces[i])) Publish(message, _subscribedActions[interfaces[i]]);
                    }
                }
            }

            public void Publish(Type type, Type keyType, object key, object message, string publisherName)
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
                                Publish(message, _subscribedActionsKeyed[type][keyType][key]);
                            }
                        }
                    }
                }
            }

            public TResponse Publish<TMessage, TResponse>(TMessage message, string publisherName)
            {
                var type = typeof(TMessage);
                lock (_lock)
                {
                    if (_subscribedFunctions.ContainsKey(type))
                    {
                        var handler = (Func<TMessage, TResponse>) _subscribedFunctions[type];
                        var result = handler.Invoke(message);
                        Publish(type, result, publisherName);
                        return result;
                    }
                }

                return default;
            }

            private void Publish(object message, FreeList<Action<object>> handlers)
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
                        _subscribedActionsKeyed.Add(type, new Dictionary<Type, Dictionary<object, FreeList<Action<object>>>>());
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

            public IDisposable Subscribe<TMessage, TResponse>(Func<TMessage, TResponse> handler)
            {
                lock (_lock)
                {
                    var type = typeof(TMessage);

                    if (!_subscribedFunctions.ContainsKey(type))
                    {
                        _subscribedFunctions.Add(type, handler);
                    }

                    return new SubscriptionType(this, type);
                }
            }

            private sealed class SubscriptionTypeKeyIndex : IDisposable
            {
                private bool _isDisposed;
                private readonly MessageBroker _messageBroker;
                private readonly int _subscriptionIndex;
                private readonly Type _type;
                private readonly Type _keyType;
                private readonly object _key;

                public SubscriptionTypeKeyIndex(MessageBroker messageBroker, Type type, Type keyType, object key, int subscriptionIndex)
                {
                    _messageBroker = messageBroker;
                    _type = type;
                    _keyType = keyType;
                    _key = key;
                    _subscriptionIndex = subscriptionIndex;
                }

                public void Dispose()
                {
                    if (!_isDisposed)
                    {
                        _isDisposed = true;
                        lock (_messageBroker._lock)
                        {
                            if (_messageBroker._subscribedActionsKeyed.ContainsKey(_type))
                            {
                                if (_messageBroker._subscribedActionsKeyed[_type].ContainsKey(_keyType))
                                {
                                    if (_messageBroker._subscribedActionsKeyed[_type][_keyType].ContainsKey(_key))
                                    {
                                        var handlers = _messageBroker._subscribedActionsKeyed[_type][_keyType][_key];
                                        handlers.Remove(_subscriptionIndex, true);

                                        if (handlers.GetCount() == 0)
                                        {
                                            handlers.Dispose();
                                            _messageBroker._subscribedActionsKeyed[_type][_keyType].Remove(_key);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            private sealed class SubscriptionTypeIndex : IDisposable
            {
                private bool _isDisposed;
                private readonly MessageBroker _messageBroker;
                private readonly int _subscriptionIndex;
                private readonly Type _type;

                public SubscriptionTypeIndex(MessageBroker messageBroker, Type type, int subscriptionIndex)
                {
                    _messageBroker = messageBroker;
                    _type = type;
                    _subscriptionIndex = subscriptionIndex;
                }

                public void Dispose()
                {
                    if (!_isDisposed)
                    {
                        _isDisposed = true;
                        lock (_messageBroker._lock)
                        {
                            if (_messageBroker._subscribedActions.ContainsKey(_type))
                            {
                                var handlers = _messageBroker._subscribedActions[_type];
                                handlers.Remove(_subscriptionIndex, true);

                                if (handlers.GetCount() == 0)
                                {
                                    handlers.Dispose();
                                    _messageBroker._subscribedActions.Remove(_type);
                                }
                            }
                        }
                    }
                }
            }
            
            private sealed class SubscriptionType : IDisposable
            {
                private bool _isDisposed;
                private readonly MessageBroker _messageBroker;
                private readonly Type _key;

                public SubscriptionType(MessageBroker messageBroker, Type key)
                {
                    _messageBroker = messageBroker;
                    _key = key;
                }

                public void Dispose()
                {
                    if (!_isDisposed)
                    {
                        _isDisposed = true;
                        lock (_messageBroker._lock)
                        {
                            if (_messageBroker._subscribedFunctions.ContainsKey(_key))
                            {
                                _messageBroker._subscribedFunctions.Remove(_key);
                            }
                        }
                    }
                }
            }
        }
    }
}