using System;
using System.Collections.Generic;
using System.Globalization;
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
            Action<string> report)
        {
            Layout layout = CadEntityAccess.OpenLayout(database, layoutName, transaction);
            var paperSpace = (BlockTableRecord)transaction.GetObject(
                layout.BlockTableRecordId,
                OpenMode.ForRead);
            List<ObjectId> entityIds = paperSpace.Cast<ObjectId>().ToList();
            ObjectId baseViewportId = PaperSpaceBaseViewportResolver.Resolve(
                entityIds,
                transaction);

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
                report(string.Format(
                    CultureInfo.InvariantCulture,
                    "[MODEL-ISOLATION-DEBUG] VIEWPORT handle={0} number={1} scale={2:R} twist={3:R} boundaryPoints={4} frozenLayers={5} bounds={6}",
                    viewport.Handle,
                    viewport.Number,
                    viewport.CustomScale,
                    viewport.TwistAngle,
                    region.BoundaryPointCount,
                    region.FrozenLayerCount,
                    region.BoundsDescription));
            }

            return regions;
        }
    }
}
