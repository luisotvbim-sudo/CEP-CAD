using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ViewportModelRegionFactory
    {
        private const double Epsilon = 1e-8;
        private readonly ViewportBoundaryReader _boundaryReader =
            new ViewportBoundaryReader();

        public ViewportModelRegion Create(
            Viewport viewport,
            Transaction transaction)
        {
            Validate(viewport);

            IReadOnlyList<Point3d> paperBoundary = _boundaryReader.Read(
                viewport,
                transaction);
            IReadOnlyList<Point2d> dcsBoundary = ToDisplayCoordinates(
                viewport,
                paperBoundary);

            return new ViewportModelRegion(
                dcsBoundary,
                CreateWorldToDisplayMatrix(viewport));
        }

        private static IReadOnlyList<Point2d> ToDisplayCoordinates(
            Viewport viewport,
            IReadOnlyList<Point3d> paperBoundary)
        {
            var result = new List<Point2d>(paperBoundary.Count);

            foreach (Point3d point in paperBoundary)
            {
                result.Add(new Point2d(
                    (point.X - viewport.CenterPoint.X) / viewport.CustomScale + viewport.ViewCenter.X,
                    (point.Y - viewport.CenterPoint.Y) / viewport.CustomScale + viewport.ViewCenter.Y));
            }

            return result;
        }

        private static Matrix3d CreateWorldToDisplayMatrix(Viewport viewport)
        {
            Vector3d direction = viewport.ViewDirection;
            Matrix3d displayToWorld = Matrix3d.PlaneToWorld(direction);
            displayToWorld = Matrix3d.Displacement(
                viewport.ViewTarget - Point3d.Origin) * displayToWorld;
            displayToWorld = Matrix3d.Rotation(
                -viewport.TwistAngle,
                direction,
                viewport.ViewTarget) * displayToWorld;

            return displayToWorld.Inverse();
        }

        private static void Validate(Viewport viewport)
        {
            double scale = viewport.CustomScale;
            if (!IsFinite(scale) || Math.Abs(scale) < Epsilon)
                throw new InvalidOperationException("CustomScale inválido.");

            if (viewport.ViewDirection.Length < Epsilon)
                throw new InvalidOperationException("ViewDirection inválido.");
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
