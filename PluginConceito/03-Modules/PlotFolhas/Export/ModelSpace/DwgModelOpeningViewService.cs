using System;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class DwgModelOpeningViewService
    {
        private const double MarginFactor = 1.12;

        public void Prepare(Database database, FolhaInfo sheet)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));

            ActivateModel(database);
            AdjustActiveViewport(database, sheet);
            SetModelExtents(database, sheet.Limites);
        }

        private static void ActivateModel(Database database)
        {
            Database previousDatabase =
                HostApplicationServices.WorkingDatabase;
            try
            {
                HostApplicationServices.WorkingDatabase = database;
                database.TileMode = true;
                LayoutManager.Current.CurrentLayout = "Model";
            }
            finally
            {
                HostApplicationServices.WorkingDatabase = previousDatabase;
            }
        }

        private static void AdjustActiveViewport(
            Database database,
            FolhaInfo sheet)
        {
            using (Transaction transaction =
                database.TransactionManager.StartTransaction())
            {
                var viewportTable = (ViewportTable)transaction.GetObject(
                    database.ViewportTableId,
                    OpenMode.ForRead);

                foreach (ObjectId viewportId in viewportTable)
                {
                    var viewport = (ViewportTableRecord)transaction.GetObject(
                        viewportId,
                        OpenMode.ForRead);
                    if (!string.Equals(
                        viewport.Name,
                        "*Active",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    viewport.UpgradeOpen();
                    FitViewport(viewport, sheet);
                    break;
                }

                transaction.Commit();
            }
        }

        private static void FitViewport(
            ViewportTableRecord viewport,
            FolhaInfo sheet)
        {
            double width = Math.Max(sheet.Largura, 1.0);
            double height = Math.Max(sheet.Altura, 1.0);
            double aspect = viewport.Height > 0.0
                ? Math.Abs(viewport.Width / viewport.Height)
                : 0.0;

            if (aspect > 0.0)
            {
                height = Math.Max(height, width / aspect);
            }

            viewport.CenterPoint = Extents2dRelations.Center(sheet.Limites);
            viewport.Height = height * MarginFactor;
            viewport.Width = aspect > 0.0
                ? viewport.Height * aspect
                : width * MarginFactor;
        }

        private static void SetModelExtents(
            Database database,
            Extents2d extents)
        {
            database.Extmin = new Point3d(
                extents.MinPoint.X,
                extents.MinPoint.Y,
                0.0);
            database.Extmax = new Point3d(
                extents.MaxPoint.X,
                extents.MaxPoint.Y,
                0.0);
        }
    }
}
