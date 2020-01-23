using System;
using System.Threading;

namespace MKOTHDiscordBot.Common
{
    public class LazyDisposable<T> : Lazy<T>, IDisposable where T : IDisposable
    {
        public LazyDisposable()
        {
        }

        public LazyDisposable(Func<T> valueFactory) : base(valueFactory)
        {
        }

        public LazyDisposable(bool isThreadSafe) : base(isThreadSafe)
        {
        }

        public LazyDisposable(LazyThreadSafetyMode mode) : base(mode)
        {
        }

        public LazyDisposable(Func<T> valueFactory, bool isThreadSafe) : base(valueFactory, isThreadSafe)
        {
        }

        public LazyDisposable(Func<T> valueFactory, LazyThreadSafetyMode mode) : base(valueFactory, mode)
        {
        }

        public void Dispose()
        {
            if (IsValueCreated)
            {
                Value.Dispose();
            }
        }
    }
}
