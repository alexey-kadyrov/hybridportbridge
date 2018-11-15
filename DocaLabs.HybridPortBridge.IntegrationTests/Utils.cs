using System;
using System.Linq;
using System.Text;

namespace DocaLabs.HybridPortBridge.IntegrationTests
{
    public class Utils
    {
        public static byte[] GenerateRandomString(int length)
        {
            var random = new Random();

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            return Encoding.UTF8.GetBytes(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}