using System;

namespace Messages
{
    internal sealed class SubscriptionTypeIndex : IDisposable
    {
        private bool _isDisposed;
        private readonly IMessageBroker _messageBroker;
        private readonly int _subscriptionIndex;
        private readonly Type _type;

        public SubscriptionTypeIndex(IMessageBroker messageBroker, Type type, int subscriptionIndex)
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
                lock (_messageBroker.Lock)
                {
                    if (_messageBroker.SubscribedActions.ContainsKey(_type))
                    {
                        var handlers = _messageBroker.SubscribedActions[_type];
                        handlers.Remove(_subscriptionIndex, true);

                        if (handlers.GetCount() == 0)
                        {
                            handlers.Dispose();
                            _messageBroker.SubscribedActions.Remove(_type);
                        }
                    }
                }
            }
        }
    }
}