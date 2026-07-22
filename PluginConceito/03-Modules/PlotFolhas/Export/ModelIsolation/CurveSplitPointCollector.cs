using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class CurveSplitPointCollector
    {
        private const double GeometricTolerance = 1e-7;
        private const double ParameterTolerance = 1e-10;

        public Point3dCollection Collect(
            Curve curve,
            IReadOnlyList<ViewportModelRegion> regions)
        {
            var rawPoints = new Point3dCollection();
            foreach (ViewportModelRegion region in regions)
                region.AddIntersections(curve, rawPoints);

            List<OrderedPoint> orderedPoints = OrderOnCurve(curve, rawPoints);
            List<OrderedPoint> unionTransitions = KeepUnionTransitions(
                curve,
                orderedPoints,
                regions);

            var result = new Point3dCollection();
            foreach (OrderedPoint point in unionTransitions) result.Add(point.Point);
            return result;
        }

        private static List<OrderedPoint> OrderOnCurve(
            Curve curve,
            Point3dCollection rawPoints)
        {
            var result = new List<OrderedPoint>();
            Point3d startPoint;
            Point3d endPoint;
            bool isClosed = IsClosed(curve);
            try
            {
                startPoint = curve.StartPoint;
                endPoint = curve.EndPoint;
            }
            catch
            {
                return result;
            }

            foreach (Point3d rawPoint in rawPoints)
            {
                try
                {
                    Point3d point = curve.GetClosestPointTo(rawPoint, false);
                    if (!isClosed &&
                        (point.DistanceTo(startPoint) <= GeometricTolerance ||
                         point.DistanceTo(endPoint) <= GeometricTolerance))
                    {
                        continue;
                    }

                    double parameter = curve.GetParameterAtPoint(point);
                    if (ContainsParameter(result, parameter)) continue;
                    result.Add(new OrderedPoint(point, parameter));
                }
                catch
                {
                    // Um ponto que nao pode ser recolocado exatamente na curva nao e seguro para corte.
                }
            }

            result.Sort((first, second) => first.Parameter.CompareTo(second.Parameter));
            return result;
        }

        private static List<OrderedPoint> KeepUnionTransitions(
            Curve curve,
            IReadOnlyList<OrderedPoint> points,
            IReadOnlyList<ViewportModelRegion> regions)
        {
            var result = new List<OrderedPoint>();
            if (points.Count == 0) return result;

            double startParameter;
            double endParameter;
            try
            {
                startParameter = curve.StartParam;
                endParameter = curve.EndParam;
            }
            catch
            {
                result.AddRange(points);
                return result;
            }

            bool isClosed = IsClosed(curve);
            double parameterRange = endParameter - startParameter;
            if (Math.Abs(parameterRange) <= ParameterTolerance)
            {
                result.AddRange(points);
                return result;
            }

            for (int index = 0; index < points.Count; index++)
            {
                double previous = index == 0
                    ? (isClosed ? points[points.Count - 1].Parameter - parameterRange : startParameter)
                    : points[index - 1].Parameter;
                double next = index == points.Count - 1
                    ? (isClosed ? points[0].Parameter + parameterRange : endParameter)
                    : points[index + 1].Parameter;

                double beforeParameter = (previous + points[index].Parameter) / 2.0;
                double afterParameter = (points[index].Parameter + next) / 2.0;

                bool beforeVisible;
                bool afterVisible;
                if (!TryIsVisibleAtParameter(
                    curve,
                    Normalize(beforeParameter, startParameter, endParameter),
                    regions,
                    out beforeVisible) ||
                    !TryIsVisibleAtParameter(
                        curve,
                        Normalize(afterParameter, startParameter, endParameter),
                        regions,
                        out afterVisible))
                {
                    result.Add(points[index]);
                    continue;
                }

                // Corta apenas quando muda entre fora e dentro da uniao das viewports.
                if (beforeVisible != afterVisible) result.Add(points[index]);
            }

            return result;
        }

        private static bool TryIsVisibleAtParameter(
            Curve curve,
            double parameter,
            IEnumerable<ViewportModelRegion> regions,
            out bool isVisible)
        {
            try
            {
                Point3d point = curve.GetPointAtParameter(parameter);
                foreach (ViewportModelRegion region in regions)
                {
                    if (!region.Contains(point)) continue;
                    isVisible = true;
                    return true;
                }

                isVisible = false;
                return true;
            }
            catch
            {
                isVisible = false;
                return false;
            }
        }

        private static bool IsClosed(Curve curve)
        {
            try
            {
                return curve.StartPoint.DistanceTo(curve.EndPoint) <= GeometricTolerance;
            }
            catch
            {
                return false;
            }
        }

        private static double Normalize(double value, double start, double end)
        {
            double range = end - start;
            while (value < start) value += range;
            while (value > end) value -= range;
            return value;
        }

        private static bool ContainsParameter(
            IEnumerable<OrderedPoint> points,
            double candidate)
        {
            foreach (OrderedPoint point in points)
            {
                double scale = Math.Max(1.0, Math.Abs(point.Parameter));
                if (Math.Abs(point.Parameter - candidate) <= ParameterTolerance * scale)
                    return true;
            }

            return false;
        }

        private sealed class OrderedPoint
        {
            public OrderedPoint(Point3d point, double parameter)
            {
                Point = point;
                Parameter = parameter;
            }

            public Point3d Point { get; private set; }
            public double Parameter { get; private set; }
        }
    }
}
