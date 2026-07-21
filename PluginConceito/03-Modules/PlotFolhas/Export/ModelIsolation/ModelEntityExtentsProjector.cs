using System;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ModelEntityExtentsProjector
    {
        private readonly Matrix3d _wcsToDcs;

        public ModelEntityExtentsProjector(Matrix3d wcsToDcs)
        {
            _wcsToDcs = wcsToDcs;
        }

        public bool TryProject(Entity entity, out Extents2d projectedExtents)
        {
            projectedExtents = default(Extents2d);
            if (entity == null) return false;

            try
            {
                Point3d[] corners = CreateCorners(entity.GeometricExtents);
                projectedExtents = ProjectCorners(corners);
                return HasFiniteCoordinates(projectedExtents);
            }
            catch
            {
                return false;
            }
        }

        private Extents2d ProjectCorners(Point3d[] corners)
        {
            Point3d first = corners[0].TransformBy(_wcsToDcs);
            double minX = first.X;
            double minY = first.Y;
            double maxX = first.X;
            double maxY = first.Y;

            for (int index = 1; index < corners.Length; index++)
            {
                Point3d point = corners[index].TransformBy(_wcsToDcs);
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }

            return new Extents2d(
                new Point2d(minX, minY),
                new Point2d(maxX, maxY));
        }

        private static Point3d[] CreateCorners(Extents3d extents)
        {
            Point3d min = extents.MinPoint;
            Point3d max = extents.MaxPoint;

            return new[]
            {
                new Point3d(min.X, min.Y, min.Z),
                new Point3d(min.X, min.Y, max.Z),
                new Point3d(min.X, max.Y, min.Z),
                new Point3d(min.X, max.Y, max.Z),
                new Point3d(max.X, min.Y, min.Z),
                new Point3d(max.X, min.Y, max.Z),
                new Point3d(max.X, max.Y, min.Z),
                new Point3d(max.X, max.Y, max.Z)
            };
        }

        private static bool HasFiniteCoordinates(Extents2d extents)
        {
            return IsFinite(extents.MinPoint.X) &&
                IsFinite(extents.MinPoint.Y) &&
                IsFinite(extents.MaxPoint.X) &&
                IsFinite(extents.MaxPoint.Y);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
