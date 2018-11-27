using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using App.Metrics;

namespace DocaLabs.HybridPortBridge
{
    public static class Helpers
    {
        public static TException Find<TException>(this Exception exception, Func<TException, bool> predicate) where TException : Exception
        {
            switch (exception)
            {
                case null:
                    return null;

                case AggregateException aggregate:
                    return aggregate.InnerExceptions.FirstOrDefault(x => x.Find(predicate) != null) as TException;

                case TException e when predicate(e):
                    return e;

                default:
                    return exception.InnerException.Find(predicate);
            }
        }

        public static bool In<T>(this T value, params T[] values)
        {
            return values.Any(x => Equals(value, x));
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

        public static string AsString(this MetricTags tags)
        {
            var keys = tags.Keys;
            if (keys == null)
                return "none";

            var values = tags.Values;
            if (values == null)
                return "none";

            var builder = new StringBuilder();

            var length = keys.Length < values.Length
                ? keys.Length
                : values.Length;

            for (var i = 0; i < length; ++i)
            {
                if (builder.Length > 0)
                    builder.Append("; ");

                builder.Append(keys[i]).Append("=").Append(values[i]);
            }

            return builder.ToString();
        }

        private static void DisposeItems<T>(this ICollection<T> items) where T : class, IDisposable
        {
            if (items == null)
                return;

            foreach (var item in items)
            {
                item.IgnoreException(x => x.Dispose());
            }
        }
    }
}
