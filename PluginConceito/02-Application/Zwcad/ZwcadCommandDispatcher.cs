using System;
using System.Windows.Input;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwcadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;

namespace PluginConceito.Application.Zwcad
{
    public sealed class ZwcadCommandDispatcher : ICommand
    {
        private readonly string _commandName;

        public ZwcadCommandDispatcher(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                throw new ArgumentException("O nome do comando é obrigatório.", nameof(commandName));
            }

            _commandName = commandName;
        }

        public event EventHandler CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object parameter)
        {
            return ZwcadApplication.DocumentManager.MdiActiveDocument != null;
        }

        public void Execute(object parameter)
        {
            Document document = ZwcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                return;
            }

            document.SendStringToExecute(_commandName + " ", true, false, true);
        }
    }
}
