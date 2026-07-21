using System;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal static class Extents2dRelations
    {
        public static bool Contains(Extents2d extents, Point2d point, double tolerance)
        {
            return point.X >= extents.MinPoint.X - tolerance &&
                point.X <= extents.MaxPoint.X + tolerance &&
                point.Y >= extents.MinPoint.Y - tolerance &&
                point.Y <= extents.MaxPoint.Y + tolerance;
        }

        public static bool Intersects(Extents2d first, Extents2d second, double tolerance)
        {
            return first.MaxPoint.X >= second.MinPoint.X - tolerance &&
                first.MinPoint.X <= second.MaxPoint.X + tolerance &&
                first.MaxPoint.Y >= second.MinPoint.Y - tolerance &&
                first.MinPoint.Y <= second.MaxPoint.Y + tolerance;
        }

        public static bool AnyCornerInside(
            Extents2d source,
            Extents2d target,
            double tolerance)
        {
            return Contains(target, source.MinPoint, tolerance) ||
                Contains(target, new Point2d(source.MinPoint.X, source.MaxPoint.Y), tolerance) ||
                Contains(target, new Point2d(source.MaxPoint.X, source.MinPoint.Y), tolerance) ||
                Contains(target, source.MaxPoint, tolerance);
        }

        public static Point2d Center(Extents2d extents)
        {
            return new Point2d(
                (extents.MinPoint.X + extents.MaxPoint.X) / 2.0,
                (extents.MinPoint.Y + extents.MaxPoint.Y) / 2.0);
        }

        public static double OverlapArea(Extents2d first, Extents2d second)
        {
            double width = Math.Min(first.MaxPoint.X, second.MaxPoint.X) -
                Math.Max(first.MinPoint.X, second.MinPoint.X);
            double height = Math.Min(first.MaxPoint.Y, second.MaxPoint.Y) -
                Math.Max(first.MinPoint.Y, second.MinPoint.Y);
            return width <= 0.0 || height <= 0.0 ? 0.0 : width * height;
        }
    }
}
