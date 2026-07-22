using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

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
            ObjectId baseViewportId = PaperSpaceBaseViewportResolver.Resolve(
                entityIdList,
                transaction);
            var selection = new PaperSpaceViewportSelection(baseViewportId);

            foreach (ObjectId entityId in entityIdList)
            {
                var viewport = CadEntityAccess.OpenEntityOrNull(transaction, entityId) as Viewport;
                if (viewport == null) continue;

                if (entityId == baseViewportId)
                {
                    selection.Add(entityId, viewport);
                    continue;
                }

                if (!BelongsToSheet(viewport, transaction, sheetRegion)) continue;

                selection.Add(entityId, viewport);
            }

            return selection;
        }

        private static bool BelongsToSheet(
            Viewport viewport,
            Transaction transaction,
            PaperSpaceSheetRegion sheetRegion)
        {
            try
            {
                if (sheetRegion.Contains(viewport.CenterPoint) || sheetRegion.Intersects(viewport))
                    return true;

                if (!viewport.NonRectClipOn || viewport.NonRectClipEntityId.IsNull)
                    return false;

                Entity clip = CadEntityAccess.OpenEntityOrNull(
                    transaction,
                    viewport.NonRectClipEntityId);
                return clip != null && sheetRegion.Intersects(clip);
            }
            catch
            {
                return false;
            }
        }
    }
}
