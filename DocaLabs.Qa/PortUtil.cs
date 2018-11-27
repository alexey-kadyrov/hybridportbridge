using System.Net;
using System.Net.Sockets;

namespace DocaLabs.Qa
{
    public static class PortUtil
    {
        public static int FindFreeTcpPort()
        {
            TcpListener tcpListener = null;

            try
            {
                tcpListener = new TcpListener(IPAddress.Loopback, 0);

                tcpListener.Start();

                return ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            }
            finally
            {
                tcpListener?.Stop();
            }
        }

        public static int[] FindFreeTcpPorts(int count)
        {
            var tcpListeners = new TcpListener[count];

            var ports = new int[count];

            try
            {
                for (var i = 0; i < count; i++)
                {
                    tcpListeners[i] = new TcpListener(IPAddress.Loopback, 0);
                    tcpListeners[i].Start();
                    ports[i] = ((IPEndPoint)tcpListeners[i].LocalEndpoint).Port;
                }

                return ports;
            }
            finally
            {
                foreach (var listener in tcpListeners)
                    listener?.Stop();
            }
        }
    }
}
