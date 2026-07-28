using System;
using System.Collections.Generic;
using System.Linq;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PaperSpaceBaseViewportSnapshot
    {
        private const double Tolerance = 1e-9;

        public PaperSpaceBaseViewportSnapshot(
            Handle sourceHandle,
            Viewport viewport)
        {
            if (viewport == null) throw new ArgumentNullException(nameof(viewport));

            SourceHandle = sourceHandle;
            CenterPoint = viewport.CenterPoint;
            Width = viewport.Width;
            Height = viewport.Height;
            CustomScale = viewport.CustomScale;
            ViewCenter = viewport.ViewCenter;
            ViewTarget = viewport.ViewTarget;
            ViewDirection = viewport.ViewDirection;
            TwistAngle = viewport.TwistAngle;
            On = viewport.On;
            PerspectiveOn = viewport.PerspectiveOn;
        }

        public Handle SourceHandle { get; }

        private Point3d CenterPoint { get; }
        private double Width { get; }
        private double Height { get; }
        private double CustomScale { get; }
        private Point2d ViewCenter { get; }
        private Point3d ViewTarget { get; }
        private Vector3d ViewDirection { get; }
        private double TwistAngle { get; }
        private bool On { get; }
        private bool PerspectiveOn { get; }

        public bool Matches(Viewport viewport)
        {
            return viewport != null &&
                AreEqual(CenterPoint, viewport.CenterPoint) &&
                AreEqual(Width, viewport.Width) &&
                AreEqual(Height, viewport.Height) &&
                AreEqual(CustomScale, viewport.CustomScale) &&
                AreEqual(ViewCenter, viewport.ViewCenter) &&
                AreEqual(ViewTarget, viewport.ViewTarget) &&
                AreEqual(ViewDirection, viewport.ViewDirection) &&
                AreEqual(TwistAngle, viewport.TwistAngle) &&
                On == viewport.On &&
                PerspectiveOn == viewport.PerspectiveOn;
        }

        private static bool AreEqual(double left, double right)
        {
            double scale = Math.Max(
                1.0,
                Math.Max(Math.Abs(left), Math.Abs(right)));
            return Math.Abs(left - right) <= Tolerance * scale;
        }

        private static bool AreEqual(Point2d left, Point2d right)
        {
            return AreEqual(left.X, right.X) &&
                AreEqual(left.Y, right.Y);
        }

        private static bool AreEqual(Point3d left, Point3d right)
        {
            return AreEqual(left.X, right.X) &&
                AreEqual(left.Y, right.Y) &&
                AreEqual(left.Z, right.Z);
        }

        private static bool AreEqual(Vector3d left, Vector3d right)
        {
            return AreEqual(left.X, right.X) &&
                AreEqual(left.Y, right.Y) &&
                AreEqual(left.Z, right.Z);
        }
    }

    internal static class PaperSpaceBaseViewportResolver
    {
        public static PaperSpaceBaseViewportSnapshot CaptureSource(
            Database sourceDatabase,
            string layoutName)
        {
            if (sourceDatabase == null)
                throw new ArgumentNullException(nameof(sourceDatabase));
            if (string.IsNullOrWhiteSpace(layoutName))
                throw new ArgumentException("Layout obrigatório.", nameof(layoutName));

            using (Transaction transaction =
                sourceDatabase.TransactionManager.StartTransaction())
            {
                Layout layout = CadEntityAccess.OpenLayout(
                    sourceDatabase,
                    layoutName,
                    transaction);
                HashSet<ObjectId> entityIds =
                    GetPaperSpaceEntityIds(layout, transaction);
                ObjectId baseViewportId = ResolveFromSourceLayout(
                    layout,
                    entityIds,
                    transaction);
                var baseViewport = CadEntityAccess.OpenEntityOrNull(
                    transaction,
                    baseViewportId) as Viewport;
                if (baseViewport == null)
                {
                    throw new InvalidOperationException(
                        "Não foi possível identificar a viewport geral do Layout " +
                        layoutName + " no desenho matriz.");
                }

                return new PaperSpaceBaseViewportSnapshot(
                    baseViewportId.Handle,
                    baseViewport);
            }
        }

        public static ObjectId ResolveCloned(
            Database clonedDatabase,
            string layoutName,
            PaperSpaceBaseViewportSnapshot sourceBaseViewport)
        {
            if (clonedDatabase == null)
                throw new ArgumentNullException(nameof(clonedDatabase));
            if (string.IsNullOrWhiteSpace(layoutName))
                throw new ArgumentException("Layout obrigatório.", nameof(layoutName));
            if (sourceBaseViewport == null)
                throw new ArgumentNullException(nameof(sourceBaseViewport));

            using (Transaction transaction =
                clonedDatabase.TransactionManager.StartTransaction())
            {
                Layout layout = CadEntityAccess.OpenLayout(
                    clonedDatabase,
                    layoutName,
                    transaction);
                HashSet<ObjectId> entityIds =
                    GetPaperSpaceEntityIds(layout, transaction);

                ObjectId mappedId;
                if (TryMapByHandle(
                        clonedDatabase,
                        sourceBaseViewport.SourceHandle,
                        out mappedId) &&
                    IsViewportInPaperSpace(
                        mappedId,
                        entityIds,
                        transaction))
                {
                    return mappedId;
                }

                List<KeyValuePair<ObjectId, Viewport>> candidates =
                    GetViewportCandidates(entityIds, transaction);
                KeyValuePair<ObjectId, Viewport> numberedBase =
                    candidates.FirstOrDefault(
                        candidate => candidate.Value.Number == 1);
                if (!numberedBase.Key.IsNull)
                    return numberedBase.Key;

                List<ObjectId> signatureMatches = candidates
                    .Where(candidate =>
                        sourceBaseViewport.Matches(candidate.Value))
                    .Select(candidate => candidate.Key)
                    .ToList();
                if (signatureMatches.Count == 1)
                    return signatureMatches[0];
                if (signatureMatches.Count > 1)
                {
                    throw new InvalidOperationException(
                        "Mais de uma viewport da cópia corresponde à viewport " +
                        "geral do Layout " + layoutName + ".");
                }

                throw new InvalidOperationException(
                    "A viewport geral do Layout " + layoutName +
                    " foi identificada no desenho matriz (handle " +
                    sourceBaseViewport.SourceHandle +
                    "), mas não foi reconhecida na cópia criada pelo Wblock.");
            }
        }

        private static HashSet<ObjectId> GetPaperSpaceEntityIds(
            Layout layout,
            Transaction transaction)
        {
            var paperSpace = (BlockTableRecord)transaction.GetObject(
                layout.BlockTableRecordId,
                OpenMode.ForRead);
            return new HashSet<ObjectId>(paperSpace.Cast<ObjectId>());
        }

        private static List<KeyValuePair<ObjectId, Viewport>>
            GetViewportCandidates(
                IEnumerable<ObjectId> entityIds,
                Transaction transaction)
        {
            var candidates =
                new List<KeyValuePair<ObjectId, Viewport>>();
            foreach (ObjectId entityId in entityIds)
            {
                var viewport = CadEntityAccess.OpenEntityOrNull(
                    transaction,
                    entityId) as Viewport;
                if (viewport == null) continue;

                candidates.Add(
                    new KeyValuePair<ObjectId, Viewport>(
                        entityId,
                        viewport));
            }

            return candidates;
        }

        private static ObjectId ResolveFromSourceLayout(
            Layout layout,
            HashSet<ObjectId> entityIds,
            Transaction transaction)
        {
            foreach (ObjectId entityId in entityIds)
            {
                var viewport = CadEntityAccess.OpenEntityOrNull(
                    transaction,
                    entityId) as Viewport;
                if (viewport != null && viewport.Number == 1)
                    return entityId;
            }

            try
            {
                ObjectIdCollection layoutViewportIds = layout.GetViewports();
                if (layoutViewportIds != null && layoutViewportIds.Count > 0)
                {
                    ObjectId documentedBaseViewportId = layoutViewportIds[0];
                    if (IsViewportInPaperSpace(
                            documentedBaseViewportId,
                            entityIds,
                            transaction))
                    {
                        return documentedBaseViewportId;
                    }
                }
            }
            catch
            {
                // O Number e a coleção podem estar incompletos no ZWCAD.
            }

            return ObjectId.Null;
        }

        private static bool IsViewportInPaperSpace(
            ObjectId objectId,
            ISet<ObjectId> entityIds,
            Transaction transaction)
        {
            return !objectId.IsNull &&
                entityIds.Contains(objectId) &&
                CadEntityAccess.OpenEntityOrNull(
                    transaction,
                    objectId) is Viewport;
        }

        private static bool TryMapByHandle(
            Database database,
            Handle handle,
            out ObjectId mappedId)
        {
            try
            {
                return database.TryGetObjectId(handle, out mappedId);
            }
            catch
            {
                mappedId = ObjectId.Null;
                return false;
            }
        }
    }
}
