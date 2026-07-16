using System;
using PluginConceito.Application.Contracts;

namespace PluginConceito.Application.Zwcad
{
    public sealed class ZwcadTelemetry : ITelemetry
    {
        private readonly IZwcadContext _zwcad;

        public ZwcadTelemetry(IZwcadContext zwcad)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
        }

        public void TrackEvent(string eventName)
        {
            _zwcad.WriteMessage("Evento: " + eventName);
        }

        public void TrackException(string operation, Exception exception)
        {
            string message = exception == null ? "erro não informado" : exception.Message;
            _zwcad.WriteMessage("Erro em " + operation + ": " + message);
        }
    }
}
