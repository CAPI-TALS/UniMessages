using System;
using System.Collections;

namespace Messages
{
    internal sealed class SubscriptionTypeKeyIndex : IDisposable
    {
        private bool _isDisposed;
        private readonly IMessageBroker _messageBroker;
        private readonly int _subscriptionIndex;
        private readonly Type _type;
        private readonly Type _keyType;
        private readonly object _key;

        public SubscriptionTypeKeyIndex(IMessageBroker messageBroker, Type type, Type keyType, object key, int subscriptionIndex)
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
                lock (_messageBroker.Lock)
                {
                    if (_messageBroker.SubscribedActionsKeyed.ContainsKey(_type))
                    {
                        if (_messageBroker.SubscribedActionsKeyed[_type].ContainsKey(_keyType))
                        {
                            if (_messageBroker.SubscribedActionsKeyed[_type][_keyType].ContainsKey(_key))
                            {
                                var handlers = _messageBroker.SubscribedActionsKeyed[_type][_keyType][_key];
                                handlers.Remove(_subscriptionIndex, true);

                                if (handlers.GetCount() == 0)
                                {
                                    handlers.Dispose();
                                    _messageBroker.SubscribedActionsKeyed[_type][_keyType].Remove(_key);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}