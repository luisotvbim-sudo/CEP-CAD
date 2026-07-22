using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ViewportModelRegion : IDisposable
    {
        private const double Epsilon = 1e-7;

        private readonly IReadOnlyList<Point3d> _points;
        private readonly Polyline _boundary;
        private readonly Extents3d _extents;
        private readonly HashSet<ObjectId> _frozenLayerIds;

        public ViewportModelRegion(
            IReadOnlyList<Point3d> points,
            IEnumerable<ObjectId> frozenLayerIds)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Count < 3)
                throw new ArgumentException("A regiao precisa de ao menos tres pontos.", nameof(points));

            _points = points;
            _boundary = CreateBoundary(points);
            _extents = CreateExtents(points);
            _frozenLayerIds = new HashSet<ObjectId>(
                frozenLayerIds ?? new ObjectId[0]);
        }

        public int BoundaryPointCount { get { return _points.Count; } }
        public int FrozenLayerCount { get { return _frozenLayerIds.Count; } }

        public string BoundsDescription
        {
            get
            {
                return string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "({0:R},{1:R})-({2:R},{3:R})",
                    _extents.MinPoint.X,
                    _extents.MinPoint.Y,
                    _extents.MaxPoint.X,
                    _extents.MaxPoint.Y);
            }
        }

        public bool IsLayerVisible(ObjectId layerId)
        {
            return layerId.IsNull || !_frozenLayerIds.Contains(layerId);
        }

        public bool Intersects(Entity entity)
        {
            Extents3d entityExtents;
            try
            {
                entityExtents = entity.GeometricExtents;
            }
            catch
            {
                return false;
            }

            if (!ExtentsOverlap(entityExtents, _extents)) return false;

            var curve = entity as Curve;
            if (curve == null) return true;

            var intersections = new Point3dCollection();
            AddIntersections(curve, intersections);
            return intersections.Count > 0 || AnySampleInside(curve);
        }

        public void AddIntersections(Entity entity, Point3dCollection target)
        {
            var line = entity as Line;
            if (line != null)
            {
                AddLineIntersections(line, target);
                return;
            }

            var circle = entity as Circle;
            if (circle != null && IsParallelToWorldZ(circle.Normal))
            {
                AddCircularIntersections(
                    circle,
                    circle.Center,
                    circle.Radius,
                    target);
                return;
            }

            var arc = entity as Arc;
            if (arc != null && IsParallelToWorldZ(arc.Normal))
            {
                AddCircularIntersections(
                    arc,
                    arc.Center,
                    arc.Radius,
                    target);
                return;
            }

            try
            {
                var intersections = new Point3dCollection();
                entity.IntersectWith(
                    _boundary,
                    Intersect.OnBothOperands,
                    intersections,
                    IntPtr.Zero,
                    IntPtr.Zero);

                foreach (Point3d point in intersections)
                {
                    if (!ContainsNearPoint(target, point)) target.Add(point);
                }
            }
            catch
            {
                // Alguns tipos proxy nao implementam IntersectWith.
            }
        }

        private void AddLineIntersections(Line line, Point3dCollection target)
        {
            Point3d lineStart = line.StartPoint;
            Point3d lineEnd = line.EndPoint;
            double lineX = lineEnd.X - lineStart.X;
            double lineY = lineEnd.Y - lineStart.Y;
            double lineLengthSquared = (lineX * lineX) + (lineY * lineY);
            if (lineLengthSquared <= Epsilon * Epsilon) return;

            for (int index = 0; index < _points.Count; index++)
            {
                Point3d edgeStart = _points[index];
                Point3d edgeEnd = _points[(index + 1) % _points.Count];
                double edgeX = edgeEnd.X - edgeStart.X;
                double edgeY = edgeEnd.Y - edgeStart.Y;
                double offsetX = edgeStart.X - lineStart.X;
                double offsetY = edgeStart.Y - lineStart.Y;
                double denominator = Cross(lineX, lineY, edgeX, edgeY);

                if (Math.Abs(denominator) > Epsilon)
                {
                    double linePosition = Cross(offsetX, offsetY, edgeX, edgeY) /
                        denominator;
                    double edgePosition = Cross(offsetX, offsetY, lineX, lineY) /
                        denominator;
                    if (linePosition < -Epsilon || linePosition > 1.0 + Epsilon ||
                        edgePosition < -Epsilon || edgePosition > 1.0 + Epsilon)
                    {
                        continue;
                    }

                    AddLinePoint(lineStart, lineEnd, linePosition, target);
                    continue;
                }

                if (Math.Abs(Cross(offsetX, offsetY, lineX, lineY)) > Epsilon)
                    continue;

                AddCollinearPoint(lineStart, lineEnd, edgeStart, lineLengthSquared, target);
                AddCollinearPoint(lineStart, lineEnd, edgeEnd, lineLengthSquared, target);
            }
        }

        private void AddCircularIntersections(
            Curve curve,
            Point3d center,
            double radius,
            Point3dCollection target)
        {
            double radiusSquared = radius * radius;
            for (int index = 0; index < _points.Count; index++)
            {
                Point3d edgeStart = _points[index];
                Point3d edgeEnd = _points[(index + 1) % _points.Count];
                double edgeX = edgeEnd.X - edgeStart.X;
                double edgeY = edgeEnd.Y - edgeStart.Y;
                double offsetX = edgeStart.X - center.X;
                double offsetY = edgeStart.Y - center.Y;

                double quadratic = (edgeX * edgeX) + (edgeY * edgeY);
                if (quadratic <= Epsilon * Epsilon) continue;

                double linear = 2.0 * ((offsetX * edgeX) + (offsetY * edgeY));
                double constant = (offsetX * offsetX) +
                    (offsetY * offsetY) - radiusSquared;
                double discriminant = (linear * linear) -
                    (4.0 * quadratic * constant);
                double discriminantTolerance = Epsilon *
                    Math.Max(1.0, radiusSquared * quadratic);
                if (discriminant < -discriminantTolerance) continue;

                discriminant = Math.Max(0.0, discriminant);
                double root = Math.Sqrt(discriminant);
                AddCircularPoint(
                    curve,
                    center.Z,
                    edgeStart,
                    edgeX,
                    edgeY,
                    (-linear - root) / (2.0 * quadratic),
                    radius,
                    target);

                if (root > Epsilon)
                {
                    AddCircularPoint(
                        curve,
                        center.Z,
                        edgeStart,
                        edgeX,
                        edgeY,
                        (-linear + root) / (2.0 * quadratic),
                        radius,
                        target);
                }
            }
        }

        private static void AddCircularPoint(
            Curve curve,
            double elevation,
            Point3d edgeStart,
            double edgeX,
            double edgeY,
            double edgePosition,
            double radius,
            Point3dCollection target)
        {
            if (edgePosition < -Epsilon || edgePosition > 1.0 + Epsilon) return;
            edgePosition = Math.Max(0.0, Math.Min(1.0, edgePosition));
            var candidate = new Point3d(
                edgeStart.X + (edgeX * edgePosition),
                edgeStart.Y + (edgeY * edgePosition),
                elevation);

            try
            {
                Point3d pointOnCurve = curve.GetClosestPointTo(candidate, false);
                double tolerance = Epsilon * Math.Max(1.0, radius);
                if (pointOnCurve.DistanceTo(candidate) > tolerance) return;
                if (!ContainsNearPoint(target, pointOnCurve)) target.Add(pointOnCurve);
            }
            catch
            {
                // O fallback geral de IntersectWith nao e usado para evitar cortes falsos.
            }
        }

        private static bool IsParallelToWorldZ(Vector3d normal)
        {
            try
            {
                return Math.Abs(normal.GetNormal().DotProduct(Vector3d.ZAxis)) >=
                    1.0 - Epsilon;
            }
            catch
            {
                return false;
            }
        }

        private static void AddCollinearPoint(
            Point3d lineStart,
            Point3d lineEnd,
            Point3d candidate,
            double lineLengthSquared,
            Point3dCollection target)
        {
            double lineX = lineEnd.X - lineStart.X;
            double lineY = lineEnd.Y - lineStart.Y;
            double position = (((candidate.X - lineStart.X) * lineX) +
                ((candidate.Y - lineStart.Y) * lineY)) / lineLengthSquared;
            if (position < -Epsilon || position > 1.0 + Epsilon) return;
            AddLinePoint(lineStart, lineEnd, position, target);
        }

        private static void AddLinePoint(
            Point3d start,
            Point3d end,
            double position,
            Point3dCollection target)
        {
            position = Math.Max(0.0, Math.Min(1.0, position));
            var point = new Point3d(
                start.X + ((end.X - start.X) * position),
                start.Y + ((end.Y - start.Y) * position),
                start.Z + ((end.Z - start.Z) * position));
            if (!ContainsNearPoint(target, point)) target.Add(point);
        }

        private static double Cross(
            double firstX,
            double firstY,
            double secondX,
            double secondY)
        {
            return (firstX * secondY) - (firstY * secondX);
        }

        public bool Contains(Point3d point)
        {
            for (int current = 0, previous = _points.Count - 1;
                current < _points.Count;
                previous = current++)
            {
                if (DistanceToSegment2d(point, _points[previous], _points[current]) <= Epsilon)
                    return true;
            }

            bool inside = false;
            for (int current = 0, previous = _points.Count - 1;
                current < _points.Count;
                previous = current++)
            {
                Point3d first = _points[current];
                Point3d second = _points[previous];
                if (((first.Y > point.Y) != (second.Y > point.Y)) &&
                    point.X < ((second.X - first.X) * (point.Y - first.Y) /
                        (second.Y - first.Y)) + first.X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        public void Dispose()
        {
            _boundary.Dispose();
        }

        private bool AnySampleInside(Curve curve)
        {
            try
            {
                double start = curve.StartParam;
                double end = curve.EndParam;
                for (int index = 0; index <= 4; index++)
                {
                    double parameter = start + ((end - start) * index / 4.0);
                    if (Contains(curve.GetPointAtParameter(parameter))) return true;
                }
            }
            catch
            {
                // Sem amostras validas, a curva nao pode ser classificada como visivel.
            }

            return false;
        }

        private static Polyline CreateBoundary(IReadOnlyList<Point3d> points)
        {
            var boundary = new Polyline();
            for (int index = 0; index < points.Count; index++)
            {
                boundary.AddVertexAt(
                    index,
                    new Point2d(points[index].X, points[index].Y),
                    0.0,
                    0.0,
                    0.0);
            }

            boundary.Closed = true;
            return boundary;
        }

        private static bool ContainsNearPoint(
            Point3dCollection points,
            Point3d candidate)
        {
            foreach (Point3d point in points)
            {
                if (point.DistanceTo(candidate) <= Epsilon) return true;
            }

            return false;
        }

        private static double DistanceToSegment2d(
            Point3d point,
            Point3d start,
            Point3d end)
        {
            double deltaX = end.X - start.X;
            double deltaY = end.Y - start.Y;
            double lengthSquared = (deltaX * deltaX) + (deltaY * deltaY);
            if (lengthSquared <= Epsilon * Epsilon)
            {
                double distanceX = point.X - start.X;
                double distanceY = point.Y - start.Y;
                return Math.Sqrt((distanceX * distanceX) + (distanceY * distanceY));
            }

            double position = ((point.X - start.X) * deltaX +
                (point.Y - start.Y) * deltaY) / lengthSquared;
            position = Math.Max(0.0, Math.Min(1.0, position));

            double nearestX = start.X + (position * deltaX);
            double nearestY = start.Y + (position * deltaY);
            double nearestDeltaX = point.X - nearestX;
            double nearestDeltaY = point.Y - nearestY;
            return Math.Sqrt(
                (nearestDeltaX * nearestDeltaX) +
                (nearestDeltaY * nearestDeltaY));
        }

        private static Extents3d CreateExtents(IReadOnlyList<Point3d> points)
        {
            double minimumX = points[0].X;
            double minimumY = points[0].Y;
            double minimumZ = points[0].Z;
            double maximumX = minimumX;
            double maximumY = minimumY;
            double maximumZ = minimumZ;

            foreach (Point3d point in points)
            {
                minimumX = Math.Min(minimumX, point.X);
                minimumY = Math.Min(minimumY, point.Y);
                minimumZ = Math.Min(minimumZ, point.Z);
                maximumX = Math.Max(maximumX, point.X);
                maximumY = Math.Max(maximumY, point.Y);
                maximumZ = Math.Max(maximumZ, point.Z);
            }

            return new Extents3d(
                new Point3d(minimumX, minimumY, minimumZ),
                new Point3d(maximumX, maximumY, maximumZ));
        }

        private static bool ExtentsOverlap(Extents3d first, Extents3d second)
        {
            return first.MaxPoint.X >= second.MinPoint.X &&
                first.MinPoint.X <= second.MaxPoint.X &&
                first.MaxPoint.Y >= second.MinPoint.Y &&
                first.MinPoint.Y <= second.MaxPoint.Y;
        }
    }
}
