using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge
{
    public static class Extensions
    {
        public static TException Find<TException>(this Exception exception, Func<TException, bool> predicate) where TException : Exception
        {
            return exception is AggregateException aggregate
                ? aggregate.InnerExceptions.FirstOrDefault(e => e is TException e1 && predicate(e1)) as TException
                : exception is TException e2 && predicate(e2)
                    ? e2
                    : null;
        }

        public static bool In<T>(this T value, params T[] values)
        {
            return values.Any(x => Equals(value, x));
        }

        private static void DisposeItems<T>(this ICollection<T> items) where T : class, IDisposable
        {
            if(items == null)
                return;

            foreach (var item in items)
            {
                item.IgnoreException(x => x.Dispose());
            }
        }

        public static void DisposeAndClear<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) where TValue : class, IDisposable
        {
            if(dictionary == null)
                return;

            dictionary.Values.DisposeItems();

            dictionary.Clear();
        }

        public static void IgnoreException<T>(this T o, params Action<T>[] actions) where T : class
        {
            if( o == null)
                return;

            foreach (var action in actions)
            {
                try
                {
                    action(o);
                }
                catch
                {
                    // intentional
                }
            }
        }

        public static async Task ExecuteAction(this SemaphoreSlim locker, Func<Task> action)
        {
            await locker.WaitAsync();

            try
            {
                await action();
            }
            finally
            {
                locker.Release();
            }
        }
    }
}
