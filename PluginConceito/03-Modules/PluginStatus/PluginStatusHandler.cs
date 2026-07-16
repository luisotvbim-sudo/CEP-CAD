using System;
using System.Reflection;
using PluginConceito.Application.Contracts;
using ZwcadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;

namespace PluginConceito.Modules.PluginStatus
{
    internal sealed class PluginStatusHandler
    {
        private readonly IModuleContext _context;

        public PluginStatusHandler(IModuleContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Execute()
        {
            Version version = typeof(PluginStatusHandler).Assembly.GetName().Version;
            string message =
                "PluginConceito " + version + " carregado com sucesso.\n\n" +
                "A arquitetura modular e a Ribbon estão ativas.";

            ZwcadApplication.ShowAlertDialog(message);
            _context.Zwcad.WriteMessage(message.Replace("\n", " "));
            _context.Telemetry.TrackEvent("CNT_PLUGIN_STATUS.Success");
        }
    }
}
