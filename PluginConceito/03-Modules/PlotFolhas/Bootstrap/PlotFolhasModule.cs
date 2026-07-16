using System;
using PluginConceito.Application.Contracts;

namespace PluginConceito.Modules.PlotFolhas
{
    public sealed class PlotFolhasModule : ICntModule
    {
        public string Id
        {
            get { return "PlotFolhas"; }
        }

        internal static PlotFolhasHandler Handler { get; private set; }

        public void Initialize(IModuleContext context)
        {
            Handler = new PlotFolhasHandler(context);
        }

        internal static void Execute()
        {
            PlotFolhasHandler handler = Handler;
            if (handler == null)
            {
                throw new InvalidOperationException("O modulo PlotFolhas ainda nao foi inicializado.");
            }

            handler.Execute();
        }
    }
}
