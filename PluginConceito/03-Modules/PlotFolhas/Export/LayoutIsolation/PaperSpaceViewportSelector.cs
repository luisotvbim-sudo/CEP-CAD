using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PaperSpaceViewportSelector
    {
        public PaperSpaceViewportSelection Select(
            IEnumerable<ObjectId> entityIds,
            Transaction transaction,
            PaperSpaceSheetRegion sheetRegion)
        {
            var entityIdList = new List<ObjectId>(entityIds);
            ObjectId baseViewportId = FindBaseViewportId(entityIdList, transaction);
            var selection = new PaperSpaceViewportSelection(baseViewportId);

            foreach (ObjectId entityId in entityIdList)
            {
                var viewport = CadEntityAccess.OpenEntityOrNull(transaction, entityId) as Viewport;
                if (viewport == null) continue;
                if (entityId != baseViewportId && !BelongsToSheet(viewport, transaction, sheetRegion))
                    continue;

                selection.Add(entityId, viewport);
            }

            return selection;
        }

        internal static ObjectId FindBaseViewportId(
            IEnumerable<ObjectId> entityIds,
            Transaction transaction)
        {
            foreach (ObjectId entityId in entityIds)
            {
                var viewport = CadEntityAccess.OpenEntityOrNull(transaction, entityId) as Viewport;
                if (viewport != null && viewport.Number == 1) return entityId;
            }

            return ObjectId.Null;
        }

        private static bool BelongsToSheet(
            Viewport viewport,
            Transaction transaction,
            PaperSpaceSheetRegion sheetRegion)
        {
            try
            {
                if (sheetRegion.Contains(viewport.CenterPoint)) return true;
                if (!viewport.NonRectClipOn || viewport.NonRectClipEntityId.IsNull) return false;

                Entity clip = CadEntityAccess.OpenEntityOrNull(
                    transaction,
                    viewport.NonRectClipEntityId);
                Extents2d clipExtents;
                return CadEntityAccess.TryGetExtents2d(clip, out clipExtents) &&
                    sheetRegion.Contains(Extents2dRelations.Center(clipExtents));
            }
            catch
            {
                return false;
            }
        }
    }
}
