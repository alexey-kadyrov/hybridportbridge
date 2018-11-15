using System;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.DataChannels;
using Serilog;

namespace DocaLabs.HybridPortBridge.Downlink
{
    public sealed class DownlinkPump
    {
        private readonly ILogger _log;
        private readonly IRelayDataChannelReader _relayReader;
        private readonly FrameDispatcher _frameDispatcher;
        private bool _stopped;

        public DownlinkPump(ILogger loggerFactory, IRelayDataChannelReader relayReader, FrameDispatcher frameDispatcher)
        {
            _log = loggerFactory?.ForContext(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));
            _relayReader = relayReader ?? throw new ArgumentNullException(nameof(relayReader));
            _frameDispatcher = frameDispatcher;
        }

        public void Stop()
        {
            _stopped = true;
        }

        public async Task<DownlinkPump> RunAsync()
        {
            try
            {
                Frame frame;

                while ((frame = await _relayReader.ReadAsync()) != null)
                {
                    if (_stopped)
                    {
                        _log.Debug("Downlink pump has been stopped");
                        return this;
                    }

#pragma warning disable 4014
                    _frameDispatcher.DispatchFrame(frame);
#pragma warning restore 4014
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "Downlink pump failed");
            }

            _log.Debug("Downlink pump completed");

            _stopped = true;

            return this;
        }
    }
}
