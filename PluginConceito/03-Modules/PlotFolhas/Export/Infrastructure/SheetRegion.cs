using System;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class SheetRegion : IDisposable
    {
        internal const double Tolerance = 1.0;

        private readonly Polyline _boundary;
        private readonly Extents2d _extents;

        public SheetRegion(Extents2d extents)
        {
            _extents = extents;
            _boundary = CreateBoundary(extents);
        }

        public bool Contains(Point2d point)
        {
            return Extents2dRelations.Contains(_extents, point, Tolerance);
        }

        public bool Contains(Point3d point)
        {
            return Contains(new Point2d(point.X, point.Y));
        }

        public bool Intersects(Entity entity)
        {
            var block = entity as BlockReference;
            if (block != null && Contains(block.Position)) return true;

            Extents2d entityExtents;
            if (!CadEntityAccess.TryGetExtents2d(entity, out entityExtents) ||
                !Extents2dRelations.Intersects(entityExtents, _extents, Tolerance))
            {
                return false;
            }

            if (Contains(Extents2dRelations.Center(entityExtents)) ||
                Extents2dRelations.Contains(
                    entityExtents,
                    Extents2dRelations.Center(_extents),
                    Tolerance) ||
                Extents2dRelations.AnyCornerInside(entityExtents, _extents, Tolerance))
            {
                return true;
            }

            return GeometryCrossesBoundary(entity);
        }

        public void Dispose()
        {
            _boundary.Dispose();
        }

        private bool GeometryCrossesBoundary(Entity entity)
        {
            try
            {
                var intersections = new Point3dCollection();
                entity.IntersectWith(
                    _boundary,
                    Intersect.OnBothOperands,
                    intersections,
                    IntPtr.Zero,
                    IntPtr.Zero);
                return intersections.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static Polyline CreateBoundary(Extents2d extents)
        {
            var boundary = new Polyline();
            boundary.AddVertexAt(0, extents.MinPoint, 0.0, 0.0, 0.0);
            boundary.AddVertexAt(1, new Point2d(extents.MaxPoint.X, extents.MinPoint.Y), 0.0, 0.0, 0.0);
            boundary.AddVertexAt(2, extents.MaxPoint, 0.0, 0.0, 0.0);
            boundary.AddVertexAt(3, new Point2d(extents.MinPoint.X, extents.MaxPoint.Y), 0.0, 0.0, 0.0);
            boundary.Closed = true;
            return boundary;
        }
    }
}
