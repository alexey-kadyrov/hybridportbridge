using System;
using System.Net;
using System.Net.Sockets;

namespace DocaLabs.HybridPortBridge.ClientAgent
{
    internal sealed class IPRange
    {
        private readonly long _begin;
        private readonly long _end;

        public IPRange(IPAddress address)
        {
            if (address.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("only IPv4 addresses permitted", nameof(address));

            _begin = _end = IPAddressToInt(address);
        }

        public IPRange(IPAddress begin, IPAddress end)
        {
            if (begin.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("only IPv4 addresses permitted", nameof(begin));

            if (end.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("only IPv4 addresses permitted", nameof(end));

            _begin = IPAddressToInt(begin);
            _end = IPAddressToInt(end);
        }

        public bool IsInRange(IPAddress address)
        {
            var ad = IPAddressToInt(address);
            return _begin <= ad && _end >= ad;
        }

        private static long IPAddressToInt(IPAddress address)
        {
            var ab = address.GetAddressBytes();
            var result = ((long)ab[0] << 24) + ((long)ab[1] << 16) + ((long)ab[2] << 8) + ab[3];
            return result;
        }
    }
}