using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal static class PaperSpaceBaseViewportResolver
    {
        public static ObjectId Resolve(
            IEnumerable<ObjectId> entityIds,
            Transaction transaction)
        {
            ObjectId firstViewportId = ObjectId.Null;

            foreach (ObjectId entityId in entityIds)
            {
                var viewport = CadEntityAccess.OpenEntityOrNull(transaction, entityId) as Viewport;
                if (viewport == null) continue;

                if (firstViewportId.IsNull) firstViewportId = entityId;
                if (viewport.Number == 1) return entityId;
            }

            // O Wblock do ZWCAD 2025 retorna Number=-1 para todas as viewports.
            // A viewport-base permanece sendo a primeira viewport do Paper Space.
            return firstViewportId;
        }
    }
}
