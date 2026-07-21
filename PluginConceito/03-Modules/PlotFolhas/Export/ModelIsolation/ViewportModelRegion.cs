using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ViewportModelRegion
    {
        private const double Epsilon = 1e-8;

        private readonly IReadOnlyList<Point2d> _boundary;
        private readonly Extents2d _boundaryExtents;
        private readonly Matrix3d _wcsToDcs;

        public ViewportModelRegion(
            IReadOnlyList<Point2d> boundary,
            Matrix3d wcsToDcs)
        {
            if (boundary == null) throw new ArgumentNullException(nameof(boundary));
            if (boundary.Count < 3) throw new ArgumentException("Contorno inválido.", nameof(boundary));

            _boundary = boundary;
            _boundaryExtents = CalculateExtents(boundary);
            _wcsToDcs = wcsToDcs;
        }

        public bool TryIntersects(Entity entity, out bool intersects)
        {
            intersects = false;
            Extents3d extents;

            try
            {
                extents = entity.GeometricExtents;
            }
            catch
            {
                return false;
            }

            Extents2d entityDcsExtents;
            if (!TryTransformExtents(extents, out entityDcsExtents))
                return false;

            intersects = PolygonIntersectsRectangle(_boundary, _boundaryExtents, entityDcsExtents);
            return true;
        }

        private bool TryTransformExtents(Extents3d extents, out Extents2d transformed)
        {
            transformed = default(Extents2d);

            try
            {
                Point3d min = extents.MinPoint;
                Point3d max = extents.MaxPoint;
                var corners = new[]
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

                if (!IsFinite(minX) || !IsFinite(minY) ||
                    !IsFinite(maxX) || !IsFinite(maxY))
                {
                    return false;
                }

                transformed = new Extents2d(
                    new Point2d(minX, minY),
                    new Point2d(maxX, maxY));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool PolygonIntersectsRectangle(
            IReadOnlyList<Point2d> polygon,
            Extents2d polygonExtents,
            Extents2d rectangle)
        {
            if (!BoundsOverlap(polygonExtents, rectangle)) return false;

            var rectanglePoints = new[]
            {
                rectangle.MinPoint,
                new Point2d(rectangle.MaxPoint.X, rectangle.MinPoint.Y),
                rectangle.MaxPoint,
                new Point2d(rectangle.MinPoint.X, rectangle.MaxPoint.Y)
            };

            foreach (Point2d point in rectanglePoints)
            {
                if (Contains(polygon, point)) return true;
            }

            foreach (Point2d point in polygon)
            {
                if (Contains(rectangle, point)) return true;
            }

            for (int polygonIndex = 0; polygonIndex < polygon.Count; polygonIndex++)
            {
                Point2d polygonStart = polygon[polygonIndex];
                Point2d polygonEnd = polygon[(polygonIndex + 1) % polygon.Count];

                for (int rectangleIndex = 0; rectangleIndex < rectanglePoints.Length; rectangleIndex++)
                {
                    Point2d rectangleStart = rectanglePoints[rectangleIndex];
                    Point2d rectangleEnd = rectanglePoints[(rectangleIndex + 1) % rectanglePoints.Length];
                    if (SegmentsIntersect(polygonStart, polygonEnd, rectangleStart, rectangleEnd))
                        return true;
                }
            }

            return false;
        }

        private static bool Contains(IReadOnlyList<Point2d> polygon, Point2d point)
        {
            bool inside = false;

            for (int current = 0, previous = polygon.Count - 1;
                current < polygon.Count;
                previous = current++)
            {
                Point2d first = polygon[current];
                Point2d second = polygon[previous];

                if (PointOnSegment(point, first, second)) return true;

                bool crossesY = (first.Y > point.Y) != (second.Y > point.Y);
                if (crossesY &&
                    point.X < (second.X - first.X) * (point.Y - first.Y) /
                    (second.Y - first.Y) + first.X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static bool Contains(Extents2d extents, Point2d point)
        {
            return point.X >= extents.MinPoint.X - Epsilon &&
                point.X <= extents.MaxPoint.X + Epsilon &&
                point.Y >= extents.MinPoint.Y - Epsilon &&
                point.Y <= extents.MaxPoint.Y + Epsilon;
        }

        private static bool SegmentsIntersect(
            Point2d firstStart,
            Point2d firstEnd,
            Point2d secondStart,
            Point2d secondEnd)
        {
            double firstA = Cross(firstStart, firstEnd, secondStart);
            double firstB = Cross(firstStart, firstEnd, secondEnd);
            double secondA = Cross(secondStart, secondEnd, firstStart);
            double secondB = Cross(secondStart, secondEnd, firstEnd);

            if (((firstA > Epsilon && firstB < -Epsilon) ||
                 (firstA < -Epsilon && firstB > Epsilon)) &&
                ((secondA > Epsilon && secondB < -Epsilon) ||
                 (secondA < -Epsilon && secondB > Epsilon)))
            {
                return true;
            }

            return Math.Abs(firstA) <= Epsilon && PointOnSegment(secondStart, firstStart, firstEnd) ||
                Math.Abs(firstB) <= Epsilon && PointOnSegment(secondEnd, firstStart, firstEnd) ||
                Math.Abs(secondA) <= Epsilon && PointOnSegment(firstStart, secondStart, secondEnd) ||
                Math.Abs(secondB) <= Epsilon && PointOnSegment(firstEnd, secondStart, secondEnd);
        }

        private static bool PointOnSegment(Point2d point, Point2d start, Point2d end)
        {
            if (Math.Abs(Cross(start, end, point)) > Epsilon) return false;

            return point.X >= Math.Min(start.X, end.X) - Epsilon &&
                point.X <= Math.Max(start.X, end.X) + Epsilon &&
                point.Y >= Math.Min(start.Y, end.Y) - Epsilon &&
                point.Y <= Math.Max(start.Y, end.Y) + Epsilon;
        }

        private static double Cross(Point2d start, Point2d end, Point2d point)
        {
            return (end.X - start.X) * (point.Y - start.Y) -
                (end.Y - start.Y) * (point.X - start.X);
        }

        private static bool BoundsOverlap(Extents2d first, Extents2d second)
        {
            return first.MaxPoint.X >= second.MinPoint.X - Epsilon &&
                first.MinPoint.X <= second.MaxPoint.X + Epsilon &&
                first.MaxPoint.Y >= second.MinPoint.Y - Epsilon &&
                first.MinPoint.Y <= second.MaxPoint.Y + Epsilon;
        }

        private static Extents2d CalculateExtents(IReadOnlyList<Point2d> points)
        {
            double minX = points[0].X;
            double minY = points[0].Y;
            double maxX = minX;
            double maxY = minY;

            foreach (Point2d point in points)
            {
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }

            return new Extents2d(new Point2d(minX, minY), new Point2d(maxX, maxY));
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
