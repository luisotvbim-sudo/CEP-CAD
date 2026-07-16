using System;
using PluginConceito.Application.Contracts;

namespace PluginConceito.Modules.PluginStatus
{
    public sealed class PluginStatusModule : ICntModule
    {
        public string Id
        {
            get { return "PluginStatus"; }
        }

        internal static PluginStatusHandler Handler { get; private set; }

        public void Initialize(IModuleContext context)
        {
            Handler = new PluginStatusHandler(context);
        }

        internal static void Execute()
        {
            PluginStatusHandler handler = Handler;
            if (handler == null)
            {
                throw new InvalidOperationException("O módulo PluginStatus ainda não foi inicializado.");
            }

            handler.Execute();
        }
    }
}
