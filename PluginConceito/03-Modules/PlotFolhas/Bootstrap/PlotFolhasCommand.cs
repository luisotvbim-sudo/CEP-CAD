using PluginConceito.Application.Contracts;
using ZwSoft.Windows;
using ZwSoft.ZwCAD.Runtime;

namespace PluginConceito.Modules.PlotFolhas
{
    public sealed class PlotFolhasCommand
    {
        private const string CommandName = "CNT_PLOT_FOLHAS";

        [CommandMethod(CommandName)]
        [CntRibbonCommand(
            CommandName,
            ButtonId = "CNT_PLOT_FOLHAS_BUTTON",
            DisplayName = "Plotar folhas",
            TabId = "CNT_GERAL",
            TabTitle = "CNT",
            PanelId = "CNT_PLOTAGEM",
            PanelTitle = "Plotagem",
            IconResource = "PluginConceito._03_Modules.PlotFolhas.Resources.PlotFolhas.png",
            ToolTip = "Mapeia as folhas do layout atual, ajuda a nomear os PDFs e plota em lote.",
            Order = 20,
            Size = RibbonItemSize.Large)]
        public void Execute()
        {
            PlotFolhasModule.Execute();
        }
    }
}
