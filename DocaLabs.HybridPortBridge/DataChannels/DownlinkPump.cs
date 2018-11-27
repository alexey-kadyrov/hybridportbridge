using System;
using System.Threading.Tasks;
using Serilog;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public sealed class DownlinkPump : IDisposable
    {
        private readonly ILogger _log;
        private readonly IRemoteDataChannelReader _remoteReader;
        private readonly FrameDispatcher _frameDispatcher;
        private bool _stopped;

        public DownlinkPump(ILogger logger, IRemoteDataChannelReader remoteReader, FrameDispatcher frameDispatcher)
        {
            _log = logger.ForContext(GetType());
            _remoteReader = remoteReader;
            _frameDispatcher = frameDispatcher;
        }

        public void Dispose()
        {
            _remoteReader.IgnoreException(x => x.Dispose());
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

                while ((frame = await _remoteReader.ReadAsync()) != null)
                {
                    if (_stopped)
                    {
                        _log.Debug("Downlink pump has been stopped");
                        return this;
                    }

                    _frameDispatcher.DispatchFrame(frame);
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
