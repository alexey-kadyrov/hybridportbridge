using System;
using System.Threading;

namespace DocaLabs.HybridPortBridge
{
    public class ConsoleAgentHost
    {
        private readonly AutoResetEvent _closing = new AutoResetEvent(false);
        private readonly IForwarder _forwarder;

        public ConsoleAgentHost(IForwarder forwarder)
        {
            _forwarder = forwarder;
        }

        public void Run()
        {
            Start();

            Console.CancelKeyPress += CancelKeyPress;

            _closing.WaitOne();

            Console.CancelKeyPress -= CancelKeyPress;
        }

        public void Start()
        {
            _forwarder.Start();
        }

        public void Stop()
        {
            _forwarder.Stop();

            _closing.Set();
        }

        private void CancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            Stop();
        }
    }
}
