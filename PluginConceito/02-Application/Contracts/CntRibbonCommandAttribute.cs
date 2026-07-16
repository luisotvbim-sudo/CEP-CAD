using System;
using ZwSoft.Windows;

namespace PluginConceito.Application.Contracts
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class CntRibbonCommandAttribute : Attribute
    {
        public CntRibbonCommandAttribute(string commandName)
        {
            CommandName = commandName;
            Order = 0;
            Size = RibbonItemSize.Standard;
        }

        public string CommandName { get; private set; }

        public string ButtonId { get; set; }

        public string DisplayName { get; set; }

        public string TabId { get; set; }

        public string TabTitle { get; set; }

        public string PanelId { get; set; }

        public string PanelTitle { get; set; }

        public string IconResource { get; set; }

        public string ToolTip { get; set; }

        public int Order { get; set; }

        public RibbonItemSize Size { get; set; }
    }
}
