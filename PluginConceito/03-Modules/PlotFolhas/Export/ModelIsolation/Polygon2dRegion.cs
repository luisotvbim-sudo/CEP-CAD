using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class Polygon2dRegion
    {
        private readonly IReadOnlyList<Point2d> _boundary;
        private readonly Extents2d _extents;

        public Polygon2dRegion(IReadOnlyList<Point2d> boundary)
        {
            if (boundary == null) throw new ArgumentNullException(nameof(boundary));
            if (boundary.Count < 3)
                throw new ArgumentException("Contorno inválido.", nameof(boundary));

            _boundary = boundary;
            _extents = CalculateExtents(boundary);
        }

        public bool Intersects(Extents2d rectangle)
        {
            if (!Geometry2dRelations.BoundsOverlap(_extents, rectangle)) return false;

            Point2d[] rectanglePoints = CreateCorners(rectangle);
            if (AnyRectangleCornerInside(rectanglePoints)) return true;
            if (AnyBoundaryPointInside(rectangle)) return true;

            return HasIntersectingEdges(rectanglePoints);
        }

        private bool AnyRectangleCornerInside(IEnumerable<Point2d> corners)
        {
            foreach (Point2d corner in corners)
            {
                if (Contains(corner)) return true;
            }

            return false;
        }

        private bool AnyBoundaryPointInside(Extents2d rectangle)
        {
            foreach (Point2d point in _boundary)
            {
                if (Geometry2dRelations.Contains(rectangle, point)) return true;
            }

            return false;
        }

        private bool HasIntersectingEdges(IReadOnlyList<Point2d> rectangle)
        {
            for (int boundaryIndex = 0; boundaryIndex < _boundary.Count; boundaryIndex++)
            {
                Point2d boundaryStart = _boundary[boundaryIndex];
                Point2d boundaryEnd = _boundary[(boundaryIndex + 1) % _boundary.Count];

                for (int rectangleIndex = 0; rectangleIndex < rectangle.Count; rectangleIndex++)
                {
                    Point2d rectangleStart = rectangle[rectangleIndex];
                    Point2d rectangleEnd = rectangle[(rectangleIndex + 1) % rectangle.Count];
                    if (Geometry2dRelations.SegmentsIntersect(
                        boundaryStart,
                        boundaryEnd,
                        rectangleStart,
                        rectangleEnd))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool Contains(Point2d point)
        {
            bool inside = false;

            for (int current = 0, previous = _boundary.Count - 1;
                current < _boundary.Count;
                previous = current++)
            {
                Point2d first = _boundary[current];
                Point2d second = _boundary[previous];

                if (Geometry2dRelations.PointOnSegment(point, first, second)) return true;

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

        private static Point2d[] CreateCorners(Extents2d rectangle)
        {
            return new[]
            {
                rectangle.MinPoint,
                new Point2d(rectangle.MaxPoint.X, rectangle.MinPoint.Y),
                rectangle.MaxPoint,
                new Point2d(rectangle.MinPoint.X, rectangle.MaxPoint.Y)
            };
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
    }
}
