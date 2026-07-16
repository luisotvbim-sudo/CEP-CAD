using ZwSoft.ZwCAD.ApplicationServices;

namespace PluginConceito.Application.Contracts
{
    public interface IZwcadContext
    {
        Document ActiveDocument { get; }

        void WriteMessage(string message);
    }
}
