using PluginConceito.Application.Contracts;
using ZwSoft.Windows;
using ZwSoft.ZwCAD.Runtime;

namespace PluginConceito.Modules.PluginStatus
{
    public sealed class PluginStatusCommand
    {
        private const string CommandName = "CNT_PLUGIN_STATUS";

        [CommandMethod(CommandName)]
        [CntRibbonCommand(
            CommandName,
            ButtonId = "CNT_PLUGIN_STATUS_BUTTON",
            DisplayName = "Status do plugin",
            TabId = "CNT_GERAL",
            TabTitle = "CNT",
            PanelId = "CNT_SISTEMA",
            PanelTitle = "Sistema",
            ToolTip = "Confirma o carregamento do plugin e da arquitetura modular.",
            Order = 10,
            Size = RibbonItemSize.Large)]
        public void Execute()
        {
            PluginStatusModule.Execute();
        }
    }
}
