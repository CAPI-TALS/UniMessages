using System;
using Messages;
using NUnit.Framework;

public class NewTestScript
{
    public interface IMessage
    {
        int Value { get; }
    }

    public struct TestMessage : IMessage
    {
        public TestMessage(int value, bool b)
        {
            Bool = b;
            Value = value;
        }

        public bool Bool { get; }
        public int Value { get; }
    }
    
    class Publisher
    {
        public void Publish()
        {
            GlobalMessages.Publish(new TestMessage(333, true), nameof(Publisher));
        }
    }
    
    class Subscriber1 : IDisposable
    {
        public bool Bool { get; set; }
        public int Value { get; set; }
        
        private readonly IDisposable _disposable;

        public Subscriber1()
        {
            var db = DisposableBag.CreateBuilder();
            GlobalMessages.Subscribe<IMessage>(OnSubscribe).AddTo(db);
            _disposable = db.Build();
        }

        private void OnSubscribe(IMessage message) => Value = message.Value;

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
    
    class Subscriber2 : IDisposable
    {
        public bool Bool { get; set; }
        public int Value { get; set; }
        
        private readonly IDisposable _disposable;

        public Subscriber2()
        {
            var db = DisposableBag.CreateBuilder();
            GlobalMessages.Subscribe<TestMessage>(OnSubscribe).AddTo(db);
            _disposable = db.Build();
        }

        private void OnSubscribe(TestMessage message)
        {
            Value = message.Value;
            Bool = message.Bool;
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
    
    // A Test behaves as an ordinary method
    [Test]
    public void NewTestScriptSimplePasses()
    {
        var sub1 = new Subscriber1();
        var sub2 = new Subscriber2();
        var pub = new Publisher();
        pub.Publish();

        Assert.True(sub1.Value == 333);
        Assert.True(!sub1.Bool);
        
        Assert.True(sub2.Value == 333);
        Assert.True(sub2.Bool);
        
        sub1.Dispose();
        sub2.Dispose();
    }
}