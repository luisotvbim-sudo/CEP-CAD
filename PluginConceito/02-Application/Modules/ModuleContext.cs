using System;
using PluginConceito.Application.Contracts;

namespace PluginConceito.Application.Modules
{
    public sealed class ModuleContext : IModuleContext
    {
        public ModuleContext(ITelemetry telemetry, IZwcadContext zwcad, IServiceProvider services)
        {
            Telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            Zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public ITelemetry Telemetry { get; private set; }

        public IZwcadContext Zwcad { get; private set; }

        public IServiceProvider Services { get; private set; }
    }
}
