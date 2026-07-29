using System;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class DwgOpeningViewService
    {
        private const double MarginFactor = 1.12;

        public void Prepare(
            Database database,
            FolhaInfo sheet,
            ObjectId baseViewportId)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));

            ActivateLayout(database, sheet.LayoutName);
            AdjustPaperViewport(database, sheet, baseViewportId);
            SetPaperExtents(database, sheet.Limites);
        }

        private static void ActivateLayout(Database database, string layoutName)
        {
            Database previousDatabase = HostApplicationServices.WorkingDatabase;
            try
            {
                HostApplicationServices.WorkingDatabase = database;
                database.TileMode = false;
                LayoutManager.Current.CurrentLayout = layoutName;
            }
            finally
            {
                HostApplicationServices.WorkingDatabase = previousDatabase;
            }
        }

        private static void AdjustPaperViewport(
            Database database,
            FolhaInfo sheet,
            ObjectId baseViewportId)
        {
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                Layout layout = CadEntityAccess.OpenLayout(database, sheet.LayoutName, transaction);
                if (!layout.IsWriteEnabled) layout.UpgradeOpen();
                layout.TabSelected = true;

                FitViewport(baseViewportId, transaction, sheet);
                transaction.Commit();
            }
        }

        private static void FitViewport(
            ObjectId viewportId,
            Transaction transaction,
            FolhaInfo sheet)
        {
            if (viewportId.IsNull) return;

            var viewport = CadEntityAccess.OpenEntityOrNull(transaction, viewportId) as Viewport;
            if (viewport == null) return;

            if (!viewport.IsWriteEnabled) viewport.UpgradeOpen();
            viewport.ViewCenter = Extents2dRelations.Center(sheet.Limites);
            viewport.ViewHeight = CalculateViewHeight(viewport, sheet);
        }

        private static double CalculateViewHeight(Viewport viewport, FolhaInfo sheet)
        {
            double viewHeight = Math.Max(sheet.Altura, 1.0);
            if (viewport.Width <= 0.0 || viewport.Height <= 0.0)
                return viewHeight * MarginFactor;

            double aspect = Math.Abs(viewport.Width / viewport.Height);
            if (aspect > 0.0)
                viewHeight = Math.Max(viewHeight, Math.Max(sheet.Largura, 1.0) / aspect);

            return viewHeight * MarginFactor;
        }

        private static void SetPaperExtents(Database database, Extents2d extents)
        {
            database.Pextmin = new Point3d(extents.MinPoint.X, extents.MinPoint.Y, 0.0);
            database.Pextmax = new Point3d(extents.MaxPoint.X, extents.MaxPoint.Y, 0.0);
        }
    }
}
