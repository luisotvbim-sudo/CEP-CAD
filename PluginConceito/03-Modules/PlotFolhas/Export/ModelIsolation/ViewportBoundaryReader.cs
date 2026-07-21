using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ViewportBoundaryReader
    {
        private const double Epsilon = 1e-8;
        private const int CircleSegments = 96;

        public IReadOnlyList<Point3d> Read(
            Viewport viewport,
            Transaction transaction)
        {
            IReadOnlyList<Point3d> clipped = TryReadClip(viewport, transaction);
            return clipped ?? CreateRectangle(viewport);
        }

        private static IReadOnlyList<Point3d> TryReadClip(
            Viewport viewport,
            Transaction transaction)
        {
            if (!viewport.NonRectClipOn || viewport.NonRectClipEntityId.IsNull)
                return null;

            try
            {
                Entity clip = CadEntityAccess.OpenEntityOrNull(
                    transaction,
                    viewport.NonRectClipEntityId);

                var polyline = clip as Polyline;
                if (IsStraightPolygon(polyline)) return ReadVertices(polyline);

                var circle = clip as Circle;
                return circle == null ? null : Approximate(circle);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsStraightPolygon(Polyline polyline)
        {
            if (polyline == null || polyline.NumberOfVertices < 3) return false;

            for (int index = 0; index < polyline.NumberOfVertices; index++)
            {
                if (Math.Abs(polyline.GetBulgeAt(index)) > Epsilon) return false;
            }

            return true;
        }

        private static IReadOnlyList<Point3d> ReadVertices(Polyline polyline)
        {
            var points = new List<Point3d>(polyline.NumberOfVertices);
            for (int index = 0; index < polyline.NumberOfVertices; index++)
                points.Add(polyline.GetPoint3dAt(index));

            return points;
        }

        private static IReadOnlyList<Point3d> Approximate(Circle circle)
        {
            var points = new List<Point3d>(CircleSegments);
            for (int index = 0; index < CircleSegments; index++)
            {
                double angle = Math.PI * 2.0 * index / CircleSegments;
                points.Add(new Point3d(
                    circle.Center.X + circle.Radius * Math.Cos(angle),
                    circle.Center.Y + circle.Radius * Math.Sin(angle),
                    circle.Center.Z));
            }

            return points;
        }

        private static IReadOnlyList<Point3d> CreateRectangle(Viewport viewport)
        {
            Point3d center = viewport.CenterPoint;
            double halfWidth = Math.Abs(viewport.Width) / 2.0;
            double halfHeight = Math.Abs(viewport.Height) / 2.0;

            if (halfWidth < Epsilon || halfHeight < Epsilon)
                throw new InvalidOperationException("Viewport sem largura ou altura válida.");

            return new[]
            {
                new Point3d(center.X - halfWidth, center.Y - halfHeight, center.Z),
                new Point3d(center.X + halfWidth, center.Y - halfHeight, center.Z),
                new Point3d(center.X + halfWidth, center.Y + halfHeight, center.Z),
                new Point3d(center.X - halfWidth, center.Y + halfHeight, center.Z)
            };
        }
    }
}
