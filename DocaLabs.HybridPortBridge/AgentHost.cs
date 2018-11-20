using System;
using System.Threading;

namespace DocaLabs.HybridPortBridge
{
    public class AgentHost
    {
        private readonly AutoResetEvent _closing = new AutoResetEvent(false);
        private readonly IForwarder _forwarder;

        public AgentHost(IForwarder forwarder)
        {
            _forwarder = forwarder;
        }

        public void Run()
        {
            Start();

            Console.CancelKeyPress += CancelKeyPress;

            _closing.WaitOne();

            _forwarder.Stop();
        }

        public void Start()
        {
            _forwarder.Start();
        }

        public void Stop()
        {
            _closing.Set();
        }

        private void CancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            Stop();
        }
    }
}
