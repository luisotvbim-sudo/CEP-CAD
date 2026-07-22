using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ViewportBoundaryProjector
    {
        private const double Epsilon = 1e-7;
        private const int CircleSegments = 96;

        public ViewportModelRegion CreateRegion(
            Viewport viewport,
            Transaction transaction)
        {
            List<Point3d> paperBoundary = ReadPaperBoundary(viewport, transaction);
            if (paperBoundary == null || paperBoundary.Count < 3) return null;

            var modelBoundary = new List<Point3d>(paperBoundary.Count);
            foreach (Point3d point in paperBoundary)
            {
                modelBoundary.Add(ProjectToModel(viewport, point));
            }

            return new ViewportModelRegion(
                modelBoundary,
                ReadFrozenLayers(viewport));
        }

        private static IReadOnlyList<ObjectId> ReadFrozenLayers(Viewport viewport)
        {
            var result = new List<ObjectId>();
            try
            {
                ObjectIdCollection frozenLayers = viewport.GetFrozenLayers();
                foreach (ObjectId layerId in frozenLayers) result.Add(layerId);
            }
            catch
            {
                // Viewports clonadas pelo Wblock podem nao expor a colecao.
            }

            return result;
        }

        private static List<Point3d> ReadPaperBoundary(
            Viewport viewport,
            Transaction transaction)
        {
            if (viewport.NonRectClipOn && !viewport.NonRectClipEntityId.IsNull)
            {
                Entity clip = CadEntityAccess.OpenEntityOrNull(
                    transaction,
                    viewport.NonRectClipEntityId);

                var polyline = clip as Polyline;
                if (polyline != null && polyline.NumberOfVertices >= 3)
                {
                    var points = new List<Point3d>(polyline.NumberOfVertices);
                    for (int index = 0; index < polyline.NumberOfVertices; index++)
                        points.Add(polyline.GetPoint3dAt(index));
                    return points;
                }

                var circle = clip as Circle;
                if (circle != null) return ApproximateCircle(circle);
            }

            return CreateRectangularBoundary(viewport);
        }

        private static List<Point3d> ApproximateCircle(Circle circle)
        {
            var points = new List<Point3d>(CircleSegments);
            for (int index = 0; index < CircleSegments; index++)
            {
                double angle = Math.PI * 2.0 * index / CircleSegments;
                points.Add(new Point3d(
                    circle.Center.X + (circle.Radius * Math.Cos(angle)),
                    circle.Center.Y + (circle.Radius * Math.Sin(angle)),
                    circle.Center.Z));
            }

            return points;
        }

        private static List<Point3d> CreateRectangularBoundary(Viewport viewport)
        {
            Point3d center = viewport.CenterPoint;
            double halfWidth = viewport.Width / 2.0;
            double halfHeight = viewport.Height / 2.0;

            return new List<Point3d>
            {
                new Point3d(center.X - halfWidth, center.Y - halfHeight, center.Z),
                new Point3d(center.X + halfWidth, center.Y - halfHeight, center.Z),
                new Point3d(center.X + halfWidth, center.Y + halfHeight, center.Z),
                new Point3d(center.X - halfWidth, center.Y + halfHeight, center.Z)
            };
        }

        // Equivale a trans Paper(3) -> DCS(2) -> UCS(1) -> WCS(0) do Lisp.
        private static Point3d ProjectToModel(Viewport viewport, Point3d paperPoint)
        {
            double scale = viewport.CustomScale;
            if (Math.Abs(scale) < Epsilon)
                throw new InvalidOperationException("Viewport com CustomScale igual a zero.");

            double dcsX = (paperPoint.X - viewport.CenterPoint.X) / scale +
                viewport.ViewCenter.X;
            double dcsY = (paperPoint.Y - viewport.CenterPoint.Y) / scale +
                viewport.ViewCenter.Y;

            Vector3d normal = viewport.ViewDirection.GetNormal();
            Vector3d xAxis = Vector3d.ZAxis.CrossProduct(normal);
            xAxis = xAxis.Length < Epsilon ? Vector3d.XAxis : xAxis.GetNormal();
            Vector3d yAxis = normal.CrossProduct(xAxis).GetNormal();

            double cosine = Math.Cos(-viewport.TwistAngle);
            double sine = Math.Sin(-viewport.TwistAngle);
            double modelX = (dcsX * cosine) - (dcsY * sine);
            double modelY = (dcsX * sine) + (dcsY * cosine);

            return viewport.ViewTarget + (xAxis * modelX) + (yAxis * modelY);
        }
    }
}
