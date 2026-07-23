namespace PluginConceito.Application.Contracts
{
    public interface IModuleContext
    {
        ITelemetry Telemetry { get; }

        IZwcadContext Zwcad { get; }
    }
}
