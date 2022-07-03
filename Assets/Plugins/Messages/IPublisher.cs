namespace Messages
{
    public interface IPublisher
    {
        void Publish<TMessage>(string publisherName = null);
        void Publish<TMessage>(TMessage message, string publisherName = null);
        void Publish<TKey, TMessage>(TKey key, string publisherName = null);
        void Publish<TKey, TMessage>(TKey key, TMessage message, string publisherName = null);
    }
}