using System;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class DwgDatabaseCloner
    {
        public Database Clone(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            using (DocumentLock documentLock = document.LockDocument())
            {
                Database clone = document.Database.Wblock();
                if (clone == null)
                    throw new InvalidOperationException("O ZWCAD não conseguiu criar a cópia do desenho ativo.");

                return clone;
            }
        }
    }
}
