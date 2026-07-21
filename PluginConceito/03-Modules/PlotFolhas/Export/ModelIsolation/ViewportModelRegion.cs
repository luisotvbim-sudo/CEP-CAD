using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ViewportModelRegion
    {
        private readonly ModelEntityExtentsProjector _extentsProjector;
        private readonly Polygon2dRegion _visibleRegion;

        public ViewportModelRegion(
            IReadOnlyList<Point2d> boundary,
            Matrix3d wcsToDcs)
        {
            if (boundary == null) throw new ArgumentNullException(nameof(boundary));

            _visibleRegion = new Polygon2dRegion(boundary);
            _extentsProjector = new ModelEntityExtentsProjector(wcsToDcs);
        }

        public bool TryIntersects(Entity entity, out bool intersects)
        {
            Extents2d projectedExtents;
            if (!_extentsProjector.TryProject(entity, out projectedExtents))
            {
                intersects = false;
                return false;
            }

            intersects = _visibleRegion.Intersects(projectedExtents);
            return true;
        }
    }
}
