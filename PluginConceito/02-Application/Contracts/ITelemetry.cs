using System;

namespace PluginConceito.Application.Contracts
{
    public interface ITelemetry
    {
        void TrackEvent(string eventName);

        void TrackException(string operation, Exception exception);
    }
}
