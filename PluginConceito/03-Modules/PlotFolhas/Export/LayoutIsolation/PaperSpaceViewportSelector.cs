using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PaperSpaceViewportSelector
    {
        public PaperSpaceViewportSelection Select(
            IEnumerable<ObjectId> entityIds,
            Transaction transaction,
            SheetRegion sheetRegion,
            ObjectId baseViewportId)
        {
            var entityIdList = new List<ObjectId>(entityIds);
            if (baseViewportId.IsNull ||
                !entityIdList.Contains(baseViewportId) ||
                !(CadEntityAccess.OpenEntityOrNull(
                    transaction,
                    baseViewportId) is Viewport))
            {
                throw new InvalidOperationException(
                    "A viewport geral mapeada nao pertence ao Paper Space da folha.");
            }

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
            SheetRegion sheetRegion)
        {
            try
            {
                if (sheetRegion.Contains(viewport.CenterPoint) ||
                    sheetRegion.Intersects(viewport))
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
