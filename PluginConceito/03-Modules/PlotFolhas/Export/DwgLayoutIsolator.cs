using System;
using System.Collections.Generic;
using System.Linq;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class DwgLayoutIsolator
    {
        private const double BoundaryTolerance = 1.0;
        private const double LayoutViewMarginFactor = 1.12;

        private readonly FolhaFormatCatalog _formats;

        public DwgLayoutIsolator(FolhaFormatCatalog formats)
        {
            _formats = formats ?? throw new ArgumentNullException(nameof(formats));
        }

        public DwgLayoutIsolationResult Isolate(Database database, FolhaInfo sheet)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            using (Polyline sheetBoundary = CreateSheetBoundary(sheet.Limites))
            {
                Layout layout = OpenLayout(database, sheet.LayoutName, transaction);
                var paperSpace = (BlockTableRecord)transaction.GetObject(
                    layout.BlockTableRecordId,
                    OpenMode.ForWrite);

                List<ObjectId> entityIds = paperSpace.Cast<ObjectId>().ToList();
                ObjectId selectedSheetId = FindSelectedSheetId(
                    database,
                    entityIds,
                    transaction,
                    sheet);

                if (selectedSheetId.IsNull)
                {
                    throw new InvalidOperationException(
                        "O bloco da folha " + sheet.Sequencia + " não foi encontrado na cópia do desenho.");
                }

                ObjectId baseViewportId = FindBasePaperViewportId(entityIds, transaction);
                var keptViewportIds = new HashSet<ObjectId>();
                var keptClipEntityIds = new HashSet<ObjectId>();
                int modelViewportsKept = 0;

                foreach (ObjectId entityId in entityIds)
                {
                    Viewport viewport = OpenEntityOrNull(transaction, entityId) as Viewport;
                    if (viewport == null) continue;

                    bool keep = entityId == baseViewportId ||
                        ViewportCenterBelongsToSheet(viewport, transaction, sheet.Limites);
                    if (!keep) continue;

                    keptViewportIds.Add(entityId);
                    AddNonRectClipEntity(viewport, keptClipEntityIds);

                    if (entityId != baseViewportId && viewport.On && !viewport.PerspectiveOn)
                        modelViewportsKept++;
                }

                int kept = 0;
                int erased = 0;

                foreach (ObjectId entityId in entityIds)
                {
                    Entity entity = OpenEntityOrNull(transaction, entityId);
                    if (entity == null || entity.IsErased) continue;

                    bool keep = ShouldKeepEntity(
                        entityId,
                        entity,
                        selectedSheetId,
                        keptViewportIds,
                        keptClipEntityIds,
                        transaction,
                        sheet,
                        sheetBoundary);

                    if (keep)
                    {
                        kept++;
                    }
                    else
                    {
                        Erase(entity);
                        erased++;
                    }
                }

                transaction.Commit();

                return new DwgLayoutIsolationResult
                {
                    EntitiesKept = kept,
                    EntitiesErased = erased,
                    ModelViewportsKept = modelViewportsKept
                };
            }
        }

        public void PrepareOpeningView(Database database, FolhaInfo sheet)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));

            ActivatePaperLayout(database, sheet.LayoutName);

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                Layout layout = OpenLayout(database, sheet.LayoutName, transaction);
                if (!layout.IsWriteEnabled) layout.UpgradeOpen();
                layout.TabSelected = true;

                var paperSpace = (BlockTableRecord)transaction.GetObject(
                    layout.BlockTableRecordId,
                    OpenMode.ForRead);
                ObjectId baseViewportId = FindBasePaperViewportId(
                    paperSpace.Cast<ObjectId>(),
                    transaction);

                FitBasePaperViewport(baseViewportId, transaction, sheet);
                transaction.Commit();
            }

            database.Pextmin = new Point3d(
                sheet.Limites.MinPoint.X,
                sheet.Limites.MinPoint.Y,
                0.0);
            database.Pextmax = new Point3d(
                sheet.Limites.MaxPoint.X,
                sheet.Limites.MaxPoint.Y,
                0.0);
        }

        private static void ActivatePaperLayout(Database database, string layoutName)
        {
            Database previousWorkingDatabase = HostApplicationServices.WorkingDatabase;
            try
            {
                HostApplicationServices.WorkingDatabase = database;
                database.TileMode = false;
                LayoutManager.Current.CurrentLayout = layoutName;
            }
            finally
            {
                HostApplicationServices.WorkingDatabase = previousWorkingDatabase;
            }
        }

        private bool ShouldKeepEntity(
            ObjectId entityId,
            Entity entity,
            ObjectId selectedSheetId,
            ISet<ObjectId> keptViewportIds,
            ISet<ObjectId> keptClipEntityIds,
            Transaction transaction,
            FolhaInfo sheet,
            Polyline sheetBoundary)
        {
            if (entityId == selectedSheetId || keptClipEntityIds.Contains(entityId))
                return true;

            if (entity is Viewport)
                return keptViewportIds.Contains(entityId);

            var block = entity as BlockReference;
            if (block != null && IsSheetBlock(block, transaction))
                return false;

            return EntityBelongsToSheet(entity, sheet.Limites, sheetBoundary);
        }

        private bool IsSheetBlock(BlockReference block, Transaction transaction)
        {
            FolhaFormat ignored;
            return _formats.TryParse(BlockNameHelper.GetEffectiveName(block, transaction), out ignored);
        }

        private static ObjectId FindSelectedSheetId(
            Database database,
            IReadOnlyList<ObjectId> entityIds,
            Transaction transaction,
            FolhaInfo sheet)
        {
            ObjectId mappedId;
            try
            {
                if (!sheet.BlockReferenceId.IsNull &&
                    database.TryGetObjectId(sheet.BlockReferenceId.Handle, out mappedId) &&
                    entityIds.Contains(mappedId))
                {
                    return mappedId;
                }
            }
            catch
            {
                // Wblock normalmente preserva handles; a busca geométrica abaixo é o fallback.
            }

            ObjectId bestId = ObjectId.Null;
            double bestOverlap = 0.0;

            foreach (ObjectId entityId in entityIds)
            {
                var block = OpenEntityOrNull(transaction, entityId) as BlockReference;
                if (block == null) continue;
                if (!string.Equals(
                    BlockNameHelper.GetEffectiveName(block, transaction),
                    sheet.BlockName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Point3d insertion = block.Position;
                if (Contains(
                    sheet.Limites,
                    new Point2d(insertion.X, insertion.Y),
                    BoundaryTolerance))
                {
                    return entityId;
                }

                Extents2d extents;
                if (!TryGetEntityExtents2d(block, out extents)) continue;

                double overlap = OverlapArea(extents, sheet.Limites);
                if (overlap > bestOverlap)
                {
                    bestOverlap = overlap;
                    bestId = entityId;
                }
            }

            return bestId;
        }

        private static bool ViewportCenterBelongsToSheet(
            Viewport viewport,
            Transaction transaction,
            Extents2d sheetExtents)
        {
            try
            {
                Point3d center = viewport.CenterPoint;
                if (Contains(sheetExtents, new Point2d(center.X, center.Y), BoundaryTolerance))
                    return true;

                if (!viewport.NonRectClipOn || viewport.NonRectClipEntityId.IsNull)
                    return false;

                Entity clip = OpenEntityOrNull(transaction, viewport.NonRectClipEntityId);
                Extents2d clipExtents;
                if (!TryGetEntityExtents2d(clip, out clipExtents)) return false;

                Point2d clipCenter = Center(clipExtents);
                return Contains(sheetExtents, clipCenter, BoundaryTolerance);
            }
            catch
            {
                return false;
            }
        }

        private static bool EntityBelongsToSheet(
            Entity entity,
            Extents2d sheetExtents,
            Polyline sheetBoundary)
        {
            var block = entity as BlockReference;
            if (block != null)
            {
                Point3d insertion = block.Position;
                if (Contains(sheetExtents, new Point2d(insertion.X, insertion.Y), BoundaryTolerance))
                    return true;
            }

            Extents2d entityExtents;
            if (!TryGetEntityExtents2d(entity, out entityExtents) ||
                !Intersects(entityExtents, sheetExtents, BoundaryTolerance))
            {
                return false;
            }

            if (Contains(sheetExtents, Center(entityExtents), BoundaryTolerance) ||
                Contains(entityExtents, Center(sheetExtents), BoundaryTolerance) ||
                AnyCornerInside(entityExtents, sheetExtents))
            {
                return true;
            }

            try
            {
                var intersections = new Point3dCollection();
                entity.IntersectWith(
                    sheetBoundary,
                    Intersect.OnBothOperands,
                    intersections,
                    IntPtr.Zero,
                    IntPtr.Zero);
                return intersections.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static void FitBasePaperViewport(
            ObjectId baseViewportId,
            Transaction transaction,
            FolhaInfo sheet)
        {
            if (baseViewportId.IsNull) return;

            var viewport = OpenEntityOrNull(transaction, baseViewportId) as Viewport;
            if (viewport == null) return;

            if (!viewport.IsWriteEnabled) viewport.UpgradeOpen();
            viewport.ViewCenter = Center(sheet.Limites);
            viewport.ViewHeight = CalculatePaperViewHeight(viewport, sheet);
        }

        private static ObjectId FindBasePaperViewportId(
            IEnumerable<ObjectId> entityIds,
            Transaction transaction)
        {
            foreach (ObjectId entityId in entityIds)
            {
                var viewport = OpenEntityOrNull(transaction, entityId) as Viewport;
                if (viewport != null && viewport.Number == 1)
                    return entityId;
            }

            return ObjectId.Null;
        }

        private static void AddNonRectClipEntity(Viewport viewport, ISet<ObjectId> clipEntityIds)
        {
            try
            {
                if (viewport.NonRectClipOn && !viewport.NonRectClipEntityId.IsNull)
                    clipEntityIds.Add(viewport.NonRectClipEntityId);
            }
            catch
            {
                // A viewport continuará válida pelo seu retângulo caso o clip esteja corrompido.
            }
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
                throw new InvalidOperationException("Layout não encontrado no DWG: " + layoutName);

            return (Layout)transaction.GetObject(layouts.GetAt(layoutName), OpenMode.ForRead);
        }

        private static Polyline CreateSheetBoundary(Extents2d extents)
        {
            var boundary = new Polyline();
            boundary.AddVertexAt(0, extents.MinPoint, 0.0, 0.0, 0.0);
            boundary.AddVertexAt(1, new Point2d(extents.MaxPoint.X, extents.MinPoint.Y), 0.0, 0.0, 0.0);
            boundary.AddVertexAt(2, extents.MaxPoint, 0.0, 0.0, 0.0);
            boundary.AddVertexAt(3, new Point2d(extents.MinPoint.X, extents.MaxPoint.Y), 0.0, 0.0, 0.0);
            boundary.Closed = true;
            return boundary;
        }

        private static bool TryGetEntityExtents2d(Entity entity, out Extents2d extents)
        {
            extents = default(Extents2d);
            if (entity == null) return false;

            try
            {
                Extents3d source = entity.GeometricExtents;
                extents = new Extents2d(
                    new Point2d(source.MinPoint.X, source.MinPoint.Y),
                    new Point2d(source.MaxPoint.X, source.MaxPoint.Y));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Entity OpenEntityOrNull(Transaction transaction, ObjectId entityId)
        {
            if (entityId.IsNull) return null;

            try
            {
                return transaction.GetObject(entityId, OpenMode.ForRead, false) as Entity;
            }
            catch
            {
                return null;
            }
        }

        private static void Erase(Entity entity)
        {
            if (!entity.IsWriteEnabled) entity.UpgradeOpen();
            entity.Erase();
        }

        private static double CalculatePaperViewHeight(Viewport viewport, FolhaInfo sheet)
        {
            double sheetWidth = Math.Max(sheet.Largura, 1.0);
            double sheetHeight = Math.Max(sheet.Altura, 1.0);
            double viewHeight = sheetHeight;

            if (viewport.Width > 0.0 && viewport.Height > 0.0)
            {
                double aspect = Math.Abs(viewport.Width / viewport.Height);
                if (aspect > 0.0) viewHeight = Math.Max(sheetHeight, sheetWidth / aspect);
            }

            return viewHeight * LayoutViewMarginFactor;
        }

        private static bool AnyCornerInside(Extents2d source, Extents2d target)
        {
            return Contains(target, source.MinPoint, BoundaryTolerance) ||
                Contains(target, new Point2d(source.MinPoint.X, source.MaxPoint.Y), BoundaryTolerance) ||
                Contains(target, new Point2d(source.MaxPoint.X, source.MinPoint.Y), BoundaryTolerance) ||
                Contains(target, source.MaxPoint, BoundaryTolerance);
        }

        private static bool Contains(Extents2d extents, Point2d point, double tolerance)
        {
            return point.X >= extents.MinPoint.X - tolerance &&
                point.X <= extents.MaxPoint.X + tolerance &&
                point.Y >= extents.MinPoint.Y - tolerance &&
                point.Y <= extents.MaxPoint.Y + tolerance;
        }

        private static bool Intersects(Extents2d first, Extents2d second, double tolerance)
        {
            return first.MaxPoint.X >= second.MinPoint.X - tolerance &&
                first.MinPoint.X <= second.MaxPoint.X + tolerance &&
                first.MaxPoint.Y >= second.MinPoint.Y - tolerance &&
                first.MinPoint.Y <= second.MaxPoint.Y + tolerance;
        }

        private static Point2d Center(Extents2d extents)
        {
            return new Point2d(
                (extents.MinPoint.X + extents.MaxPoint.X) / 2.0,
                (extents.MinPoint.Y + extents.MaxPoint.Y) / 2.0);
        }

        private static double OverlapArea(Extents2d first, Extents2d second)
        {
            double width = Math.Min(first.MaxPoint.X, second.MaxPoint.X) -
                Math.Max(first.MinPoint.X, second.MinPoint.X);
            double height = Math.Min(first.MaxPoint.Y, second.MaxPoint.Y) -
                Math.Max(first.MinPoint.Y, second.MinPoint.Y);
            return width <= 0.0 || height <= 0.0 ? 0.0 : width * height;
        }
    }

    internal sealed class DwgLayoutIsolationResult
    {
        public int EntitiesKept { get; internal set; }

        public int EntitiesErased { get; internal set; }

        public int ModelViewportsKept { get; internal set; }
    }
}
