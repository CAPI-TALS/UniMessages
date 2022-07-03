using System;
using System.Collections.Generic;

namespace Messages
{
    internal interface IMessageBroker
    {
        object Lock { get; }
        Dictionary<Type, Dictionary<Type, Dictionary<object, FreeList<Action<object>>>>> SubscribedActionsKeyed { get; }
        Dictionary<Type, FreeList<Action<object>>> SubscribedActions { get; }
    }
}