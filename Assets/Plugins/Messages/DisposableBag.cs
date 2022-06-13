using System;
using System.Collections.Generic;

namespace Messages
{
    public static class DisposableBag
    {
        public static IDisposable Create(params IDisposable[] disposables)
        {
            return new Disposable(disposables);
        }

        public static DisposableBagBuilder CreateBuilder()
        {
            return new DisposableBagBuilder();
        }

        public static DisposableBagBuilder CreateBuilder(int initialCapacity)
        {
            return new DisposableBagBuilder(initialCapacity);
        }

        public static void AddTo(this IDisposable disposable, DisposableBagBuilder disposableBag)
        {
            disposableBag.Add(disposable);
        }

        private sealed class Disposable : IDisposable
        {
            private bool _disposed;
            private readonly IDisposable[] _disposables;

            public Disposable(IDisposable[] disposables)
            {
                _disposables = disposables;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    foreach (var item in _disposables)
                    {
                        item.Dispose();
                    }
                }
            }
        }
    }

    public class DisposableBagBuilder
    {
        private readonly List<IDisposable> _disposables;

        internal DisposableBagBuilder()
        {
            _disposables = new List<IDisposable>();
        }

        internal DisposableBagBuilder(int initialCapacity)
        {
            _disposables = new List<IDisposable>(initialCapacity);
        }

        public void Add(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        public IDisposable Build()
        {
            return DisposableBag.Create(_disposables.ToArray());
        }

        public void Clear()
        {
            foreach (var item in _disposables)
            {
                item.Dispose();
            }

            _disposables.Clear();
        }
    }
}