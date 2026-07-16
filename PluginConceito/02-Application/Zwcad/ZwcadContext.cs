using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwcadApplication = ZwSoft.ZwCAD.ApplicationServices.Application;

namespace PluginConceito.Application.Zwcad
{
    public sealed class ZwcadContext : IZwcadContext
    {
        public Document ActiveDocument
        {
            get { return ZwcadApplication.DocumentManager.MdiActiveDocument; }
        }

        public void WriteMessage(string message)
        {
            Document document = ActiveDocument;
            if (document == null)
            {
                return;
            }

            document.Editor.WriteMessage("\n[CNT] " + message);
        }
    }
}
