using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ViewportModelRegionProvider
    {
        private const double Epsilon = 1e-8;
        private const int CircleSegments = 96;

        public IReadOnlyList<ViewportModelRegion> Create(
            Database database,
            string layoutName,
            Transaction transaction)
        {
            Layout layout = OpenLayout(database, layoutName, transaction);
            var paperSpace = (BlockTableRecord)transaction.GetObject(
                layout.BlockTableRecordId,
                OpenMode.ForRead);
            List<ViewportEntry> viewports = FindViewports(paperSpace, transaction);
            ObjectId baseViewportId = FindBaseViewportId(viewports);
            var regions = new List<ViewportModelRegion>();

            foreach (ViewportEntry entry in viewports)
            {
                if (entry.Id == baseViewportId || !entry.Viewport.On) continue;

                if (entry.Viewport.PerspectiveOn)
                {
                    throw new InvalidOperationException(
                        "A folha contém uma viewport em perspectiva. " +
                        "O Model não foi alterado porque esse tipo de vista ainda não pode ser isolado com segurança.");
                }

                try
                {
                    regions.Add(CreateRegion(entry.Viewport, transaction));
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        "Não foi possível calcular a região da viewport " + entry.Viewport.Number +
                        ". O Model não foi alterado.",
                        exception);
                }
            }

            return regions;
        }

        private static ViewportModelRegion CreateRegion(
            Viewport viewport,
            Transaction transaction)
        {
            double scale = viewport.CustomScale;
            if (!IsFinite(scale) || Math.Abs(scale) < Epsilon)
                throw new InvalidOperationException("CustomScale inválido.");

            Vector3d direction = viewport.ViewDirection;
            if (direction.Length < Epsilon)
                throw new InvalidOperationException("ViewDirection inválido.");

            IReadOnlyList<Point3d> paperBoundary = GetPaperBoundary(viewport, transaction);
            var dcsBoundary = new List<Point2d>(paperBoundary.Count);

            foreach (Point3d paperPoint in paperBoundary)
            {
                dcsBoundary.Add(new Point2d(
                    (paperPoint.X - viewport.CenterPoint.X) / scale + viewport.ViewCenter.X,
                    (paperPoint.Y - viewport.CenterPoint.Y) / scale + viewport.ViewCenter.Y));
            }

            Matrix3d dcsToWcs = Matrix3d.PlaneToWorld(direction);
            dcsToWcs = Matrix3d.Displacement(viewport.ViewTarget - Point3d.Origin) * dcsToWcs;
            dcsToWcs = Matrix3d.Rotation(
                -viewport.TwistAngle,
                direction,
                viewport.ViewTarget) * dcsToWcs;

            return new ViewportModelRegion(dcsBoundary, dcsToWcs.Inverse());
        }

        private static IReadOnlyList<Point3d> GetPaperBoundary(
            Viewport viewport,
            Transaction transaction)
        {
            IReadOnlyList<Point3d> clipped = TryGetClippedBoundary(viewport, transaction);
            return clipped ?? CreateRectangularBoundary(viewport);
        }

        private static IReadOnlyList<Point3d> TryGetClippedBoundary(
            Viewport viewport,
            Transaction transaction)
        {
            try
            {
                if (!viewport.NonRectClipOn || viewport.NonRectClipEntityId.IsNull)
                    return null;

                Entity clip = transaction.GetObject(
                    viewport.NonRectClipEntityId,
                    OpenMode.ForRead,
                    false) as Entity;

                var polyline = clip as Polyline;
                if (polyline != null && polyline.NumberOfVertices >= 3 && !HasCurvedSegments(polyline))
                {
                    var points = new List<Point3d>(polyline.NumberOfVertices);
                    for (int index = 0; index < polyline.NumberOfVertices; index++)
                        points.Add(polyline.GetPoint3dAt(index));
                    return points;
                }

                var circle = clip as Circle;
                if (circle != null) return ApproximateCircle(circle);

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool HasCurvedSegments(Polyline polyline)
        {
            for (int index = 0; index < polyline.NumberOfVertices; index++)
            {
                if (Math.Abs(polyline.GetBulgeAt(index)) > Epsilon) return true;
            }

            return false;
        }

        private static IReadOnlyList<Point3d> ApproximateCircle(Circle circle)
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

        private static IReadOnlyList<Point3d> CreateRectangularBoundary(Viewport viewport)
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

        private static Layout OpenLayout(
            Database database,
            string layoutName,
            Transaction transaction)
        {
            var layouts = (DBDictionary)transaction.GetObject(
                database.LayoutDictionaryId,
                OpenMode.ForRead);
            if (!layouts.Contains(layoutName))
                throw new InvalidOperationException("Layout não encontrado: " + layoutName);

            return (Layout)transaction.GetObject(layouts.GetAt(layoutName), OpenMode.ForRead);
        }

        private static List<ViewportEntry> FindViewports(
            BlockTableRecord paperSpace,
            Transaction transaction)
        {
            var result = new List<ViewportEntry>();
            foreach (ObjectId entityId in paperSpace)
            {
                try
                {
                    var viewport = transaction.GetObject(
                        entityId,
                        OpenMode.ForRead,
                        false) as Viewport;
                    if (viewport != null) result.Add(new ViewportEntry(entityId, viewport));
                }
                catch
                {
                    // Uma entidade inválida do Paper Space não representa uma viewport elegível.
                }
            }

            return result;
        }

        private static ObjectId FindBaseViewportId(IEnumerable<ViewportEntry> viewports)
        {
            foreach (ViewportEntry entry in viewports)
            {
                if (entry.Viewport.Number == 1) return entry.Id;
            }

            return ObjectId.Null;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private sealed class ViewportEntry
        {
            public ViewportEntry(ObjectId id, Viewport viewport)
            {
                Id = id;
                Viewport = viewport;
            }

            public ObjectId Id { get; }

            public Viewport Viewport { get; }
        }
    }
}
