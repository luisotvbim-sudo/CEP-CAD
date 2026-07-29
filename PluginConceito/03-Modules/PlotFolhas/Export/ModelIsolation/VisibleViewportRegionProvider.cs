using System;
using System.Collections.Generic;
using System.Linq;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class VisibleViewportRegionProvider
    {
        private readonly ViewportBoundaryProjector _boundaryProjector;

        public VisibleViewportRegionProvider()
        {
            _boundaryProjector = new ViewportBoundaryProjector();
        }

        public List<ViewportModelRegion> Create(
            Database database,
            string layoutName,
            Transaction transaction,
            ObjectId baseViewportId)
        {
            Layout layout = CadEntityAccess.OpenLayout(database, layoutName, transaction);
            var paperSpace = (BlockTableRecord)transaction.GetObject(
                layout.BlockTableRecordId,
                OpenMode.ForRead);
            List<ObjectId> entityIds = paperSpace.Cast<ObjectId>().ToList();
            if (baseViewportId.IsNull ||
                !entityIds.Contains(baseViewportId) ||
                !(CadEntityAccess.OpenEntityOrNull(
                    transaction,
                    baseViewportId) is Viewport))
            {
                throw new InvalidOperationException(
                    "A viewport geral mapeada nao pertence ao Layout " +
                    layoutName + ".");
            }

            var regions = new List<ViewportModelRegion>();
            foreach (ObjectId entityId in entityIds)
            {
                if (entityId == baseViewportId) continue;

                var viewport = CadEntityAccess.OpenEntityOrNull(
                    transaction,
                    entityId) as Viewport;
                if (viewport == null || !viewport.On || viewport.PerspectiveOn) continue;

                ViewportModelRegion region = _boundaryProjector.CreateRegion(
                    viewport,
                    transaction);
                if (region == null) continue;

                regions.Add(region);
            }

            return regions;
        }
    }
}
