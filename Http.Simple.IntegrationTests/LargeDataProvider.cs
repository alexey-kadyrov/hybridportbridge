﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Http.Simple.IntegrationTests
{
    internal static class LargeDataProvider
    {
        private static int _counter;
        private static readonly List<byte[]> Data;

        static LargeDataProvider()
        {
            var random = new Random();

            Data = new List<byte[]>();

            for (var i = 0; i < 5; i++)
            {
                var count = random.Next(137531, 183459);

                var data = new byte[count];

                random.NextBytes(data);

                Data.Add(data);
            }

            var c = random.Next(125531, 153753);
            Data.Add(new byte[c]);

            c = random.Next(125531, 153753);
            var d = new byte[c];
            for (var i = 0; i < c; i++)
            {
                d[i] = 255;
            }
            Data.Add(d);
        }

        public static byte[] Next()
        {
            var i = Interlocked.Increment(ref _counter) % Data.Count;

            return Data[i];
        }

        public static async Task<bool> Compare(byte[] expected, HttpContent received)
        {
            using(var actual = new MemoryStream())
            {
                await received.CopyToAsync(actual);

                if (expected.Length != actual.Length)
                    return false;

                var d2 = actual.GetBuffer();

                return !expected.Where((b, i) => b != d2[i]).Any();
            }
        }
    }
}
