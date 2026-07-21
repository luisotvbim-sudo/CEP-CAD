using System;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal static class Geometry2dRelations
    {
        private const double Epsilon = 1e-8;

        public static bool BoundsOverlap(Extents2d first, Extents2d second)
        {
            return first.MaxPoint.X >= second.MinPoint.X - Epsilon &&
                first.MinPoint.X <= second.MaxPoint.X + Epsilon &&
                first.MaxPoint.Y >= second.MinPoint.Y - Epsilon &&
                first.MinPoint.Y <= second.MaxPoint.Y + Epsilon;
        }

        public static bool Contains(Extents2d extents, Point2d point)
        {
            return point.X >= extents.MinPoint.X - Epsilon &&
                point.X <= extents.MaxPoint.X + Epsilon &&
                point.Y >= extents.MinPoint.Y - Epsilon &&
                point.Y <= extents.MaxPoint.Y + Epsilon;
        }

        public static bool SegmentsIntersect(
            Point2d firstStart,
            Point2d firstEnd,
            Point2d secondStart,
            Point2d secondEnd)
        {
            double firstA = Cross(firstStart, firstEnd, secondStart);
            double firstB = Cross(firstStart, firstEnd, secondEnd);
            double secondA = Cross(secondStart, secondEnd, firstStart);
            double secondB = Cross(secondStart, secondEnd, firstEnd);

            bool crosses = HasOppositeSigns(firstA, firstB) &&
                HasOppositeSigns(secondA, secondB);
            if (crosses) return true;

            return Math.Abs(firstA) <= Epsilon && PointOnSegment(secondStart, firstStart, firstEnd) ||
                Math.Abs(firstB) <= Epsilon && PointOnSegment(secondEnd, firstStart, firstEnd) ||
                Math.Abs(secondA) <= Epsilon && PointOnSegment(firstStart, secondStart, secondEnd) ||
                Math.Abs(secondB) <= Epsilon && PointOnSegment(firstEnd, secondStart, secondEnd);
        }

        public static bool PointOnSegment(Point2d point, Point2d start, Point2d end)
        {
            if (Math.Abs(Cross(start, end, point)) > Epsilon) return false;

            return point.X >= Math.Min(start.X, end.X) - Epsilon &&
                point.X <= Math.Max(start.X, end.X) + Epsilon &&
                point.Y >= Math.Min(start.Y, end.Y) - Epsilon &&
                point.Y <= Math.Max(start.Y, end.Y) + Epsilon;
        }

        private static bool HasOppositeSigns(double first, double second)
        {
            return first > Epsilon && second < -Epsilon ||
                first < -Epsilon && second > Epsilon;
        }

        private static double Cross(Point2d start, Point2d end, Point2d point)
        {
            return (end.X - start.X) * (point.Y - start.Y) -
                (end.Y - start.Y) * (point.X - start.X);
        }
    }
}
