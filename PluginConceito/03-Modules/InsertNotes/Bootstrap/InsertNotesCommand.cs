using PluginConceito.Application.Contracts;
using ZwSoft.Windows;
using ZwSoft.ZwCAD.Runtime;

namespace PluginConceito.Modules.InsertNotes
{
    public sealed class InsertNotesCommand
    {
        private const string CommandName = "CNT_INSERT_NOTES";

        [CommandMethod(CommandName)]
        [CntRibbonCommand(
            CommandName,
            ButtonId = "CNT_INSERT_NOTES_BUTTON",
            DisplayName = "Inserir notas",
            TabId = "CNT_GERAL",
            TabTitle = "CNT",
            PanelId = "CNT_INSERIR",
            PanelTitle = "Inserir",
            ToolTip = "Insere blocos de nota no desenho conforme a disciplina selecionada.",
            Order = 30,
            Size = RibbonItemSize.Large)]
        public void Execute()
        {
            InsertNotesModule.Execute();
        }
    }
}
