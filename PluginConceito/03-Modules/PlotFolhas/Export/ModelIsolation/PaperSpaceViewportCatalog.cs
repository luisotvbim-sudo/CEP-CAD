using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PaperSpaceViewportCatalog
    {
        public IReadOnlyList<PaperSpaceViewport> Find(
            Database database,
            string layoutName,
            Transaction transaction)
        {
            Layout layout = CadEntityAccess.OpenLayout(database, layoutName, transaction);
            var paperSpace = (BlockTableRecord)transaction.GetObject(
                layout.BlockTableRecordId,
                OpenMode.ForRead);
            var result = new List<PaperSpaceViewport>();

            foreach (ObjectId entityId in paperSpace)
            {
                Viewport viewport = OpenViewportOrNull(transaction, entityId);
                if (viewport != null)
                    result.Add(new PaperSpaceViewport(entityId, viewport));
            }

            return result;
        }

        private static Viewport OpenViewportOrNull(
            Transaction transaction,
            ObjectId entityId)
        {
            try
            {
                return transaction.GetObject(
                    entityId,
                    OpenMode.ForRead,
                    false) as Viewport;
            }
            catch
            {
                return null;
            }
        }
    }
}
