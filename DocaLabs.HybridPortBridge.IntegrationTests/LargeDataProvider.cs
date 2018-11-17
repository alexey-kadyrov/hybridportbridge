using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DocaLabs.HybridPortBridge.IntegrationTests
{
    public static class LargeDataProvider
    {
        private static int _counter;
        private static readonly List<byte[]> Data;

        static LargeDataProvider()
        {
            var random = new Random();

            Data = new List<byte[]>();

            for (var i = 0; i < 5; i++)
            {
                var count = random.Next(125531, 337459);

                var data = new byte[count];

                random.NextBytes(data);

                Data.Add(data);
            }

            var c = random.Next(125531, 337459);
            Data.Add(new byte[c]);

            c = random.Next(125531, 337459);
            var d = new byte[c];
            for (var i = 0; i < c; i++)
            {
                d[i] = 255;
            }
            Data.Add(d);
        }

        public static MemoryStream Next()
        {
            var i = Interlocked.Increment(ref _counter) % Data.Count;
            return new MemoryStream(Data[i]);
        }

        public static bool Compare(MemoryStream expected, MemoryStream actual)
        {
            if (expected.Length != actual.Length)
                return false;

            var d1 = expected.GetBuffer();
            var d2 = actual.GetBuffer();

            return !d1.Where((t, i) => t != d2[i]).Any();
        }
    }
}
