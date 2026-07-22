using System;
using System.Collections.Generic;
using System.Globalization;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class CurvePieceClassifier
    {
        public bool TryClassify(
            Curve piece,
            IEnumerable<ViewportModelRegion> regions,
            out bool isVisible,
            out string diagnostic)
        {
            Point3d point;
            string method;
            if (!TryGetInteriorPoint(piece, out point, out method))
            {
                isVisible = false;
                diagnostic = "interior-point-unavailable";
                return false;
            }

            isVisible = IsInsideAnyRegion(point, regions);
            diagnostic = string.Format(
                CultureInfo.InvariantCulture,
                "{0}@({1:R},{2:R})={3}",
                method,
                point.X,
                point.Y,
                isVisible ? "in" : "out");
            return true;
        }

        private static bool TryGetInteriorPoint(
            Curve curve,
            out Point3d point,
            out string method)
        {
            var line = curve as Line;
            if (line != null)
            {
                Point3d start = line.StartPoint;
                Point3d end = line.EndPoint;
                point = new Point3d(
                    (start.X + end.X) / 2.0,
                    (start.Y + end.Y) / 2.0,
                    (start.Z + end.Z) / 2.0);
                method = "line-midpoint";
                return IsFinite(point);
            }

            var arc = curve as Arc;
            if (arc != null)
            {
                try
                {
                    double parameter = (arc.StartParam + arc.EndParam) / 2.0;
                    point = arc.GetPointAtParameter(parameter);
                    if (IsFinite(point))
                    {
                        method = "arc-parameter";
                        return true;
                    }
                }
                catch
                {
                    // Continua pela distancia para arcos temporarios nao parametrizaveis.
                }
            }

            try
            {
                double startDistance = curve.GetDistanceAtParameter(curve.StartParam);
                double endDistance = curve.GetDistanceAtParameter(curve.EndParam);
                point = curve.GetPointAtDist(
                    startDistance + ((endDistance - startDistance) / 2.0));
                if (IsFinite(point))
                {
                    method = "distance";
                    return true;
                }
            }
            catch
            {
                // Algumas curvas temporarias do ZWCAD nao aceitam distancia.
            }

            try
            {
                double parameter = (curve.StartParam + curve.EndParam) / 2.0;
                point = curve.GetPointAtParameter(parameter);
                if (IsFinite(point))
                {
                    method = "parameter";
                    return true;
                }
            }
            catch
            {
                // Ultima tentativa: meio da corda projetado sobre a curva.
            }

            try
            {
                Point3d start = curve.StartPoint;
                Point3d end = curve.EndPoint;
                var chordMiddle = new Point3d(
                    (start.X + end.X) / 2.0,
                    (start.Y + end.Y) / 2.0,
                    (start.Z + end.Z) / 2.0);
                point = curve.GetClosestPointTo(chordMiddle, false);
                if (IsFinite(point))
                {
                    method = "closest";
                    return true;
                }
            }
            catch
            {
                // O chamador preservara a curva original.
            }

            point = Point3d.Origin;
            method = "unavailable";
            return false;
        }

        private static bool IsInsideAnyRegion(
            Point3d point,
            IEnumerable<ViewportModelRegion> regions)
        {
            foreach (ViewportModelRegion region in regions)
            {
                if (region.Contains(point)) return true;
            }

            return false;
        }

        private static bool IsFinite(Point3d point)
        {
            return !double.IsNaN(point.X) && !double.IsInfinity(point.X) &&
                !double.IsNaN(point.Y) && !double.IsInfinity(point.Y) &&
                !double.IsNaN(point.Z) && !double.IsInfinity(point.Z);
        }
    }
}
