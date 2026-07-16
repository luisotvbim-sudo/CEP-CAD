using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class DwgExportService
    {
        private const double SheetTolerance = 20.0;
        private const double ViewportSelectionTolerance = 1.0;
        private const double LayoutViewMarginFactor = 1.12;

        private readonly IZwcadContext _zwcad;
        private readonly FolhaFormatCatalog _formats;

        public DwgExportService(IZwcadContext zwcad, FolhaFormatCatalog formats)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
            _formats = formats ?? throw new ArgumentNullException(nameof(formats));
        }

        public int ExportSheets(
            IReadOnlyList<FolhaInfo> sheets,
            string outputFolder,
            bool overwriteExisting,
            Action<string> progress)
        {
            if (sheets == null || sheets.Count == 0) return 0;

            Document document = _zwcad.ActiveDocument;
            if (document == null) throw new InvalidOperationException("Não existe desenho ativo.");

            string sourcePath = document.Name;
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                throw new InvalidOperationException(
                    "Para gerar DWG por folha, salve o desenho atual antes de executar o comando.");
            }

            Directory.CreateDirectory(outputFolder);
            int exported = 0;
            for (int index = 0; index < sheets.Count; index++)
            {
                FolhaInfo sheet = sheets[index];
                string outputPath = Path.Combine(outputFolder, Path.ChangeExtension(sheet.NomeArquivo, ".dwg"));
                EnsureOutputDoesNotReplaceSource(sourcePath, outputPath);
                DeleteExistingOutput(outputPath, overwriteExisting);

                Report(progress, string.Format(
                    "DWG folha {0}/{1}: preparando {2}",
                    index + 1,
                    sheets.Count,
                    outputPath));

                ExportSingleSheet(sourcePath, outputPath, sheet, progress);
                exported++;
            }

            Report(progress, "DWG concluído: " + exported + " arquivo(s).");
            return exported;
        }

        private void ExportSingleSheet(
            string sourcePath,
            string outputPath,
            FolhaInfo sheet,
            Action<string> progress)
        {
            using (var database = new Database(false, true))
            {
                Report(progress, "DWG folha " + sheet.Sequencia + ": lendo desenho salvo.");
                database.ReadDwgFile(sourcePath, FileOpenMode.OpenForReadAndAllShare, false, string.Empty);
                database.CloseInput(true);

                using (Transaction transaction = database.TransactionManager.StartTransaction())
                {
                    Layout layout = GetLayout(database, transaction, sheet.LayoutName);
                    var paperSpace = (BlockTableRecord)transaction.GetObject(
                        layout.BlockTableRecordId,
                        OpenMode.ForWrite);

                    CleanLayout(paperSpace, transaction, sheet, progress);
                    FitLayoutViewToSheet(paperSpace, transaction, sheet, progress);

                    // O Model Space não é aberto nem alterado neste fluxo.
                    Report(progress, "DWG folha " + sheet.Sequencia + ": Model preservado integralmente.");
                    transaction.Commit();
                }

                Report(progress, "DWG folha " + sheet.Sequencia + ": salvando arquivo.");
                database.SaveAs(outputPath, DwgVersion.Current);
            }

            if (!File.Exists(outputPath))
            {
                throw new IOException("DWG não foi gerado: " + outputPath);
            }

            Report(progress, "DWG folha " + sheet.Sequencia + ": arquivo gerado.");
        }

        private void CleanLayout(
            BlockTableRecord paperSpace,
            Transaction transaction,
            FolhaInfo sheet,
            Action<string> progress)
        {
            List<ObjectId> entityIds = paperSpace.Cast<ObjectId>().ToList();
            int kept = 0;
            int erased = 0;
            int viewportsKept = 0;
            int viewportsErased = 0;

            foreach (ObjectId entityId in entityIds)
            {
                var entity = transaction.GetObject(entityId, OpenMode.ForRead, false) as Entity;
                if (entity == null) continue;

                var viewport = entity as Viewport;
                if (viewport != null)
                {
                    if (IsBasePaperViewport(viewport) || ViewportBelongsToSheet(viewport, sheet))
                    {
                        kept++;
                        viewportsKept++;
                    }
                    else
                    {
                        Erase(entity);
                        erased++;
                        viewportsErased++;
                    }
                    continue;
                }

                var block = entity as BlockReference;
                if (block != null && IsSheetBlock(block, transaction))
                {
                    if (IsSelectedSheetBlock(block, transaction, sheet))
                    {
                        kept++;
                    }
                    else
                    {
                        Erase(entity);
                        erased++;
                    }
                    continue;
                }

                if (EntityIntersectsSheet(entity, sheet))
                {
                    kept++;
                }
                else
                {
                    Erase(entity);
                    erased++;
                }
            }

            Report(progress, string.Format(
                "DWG folha {0}: Layout limpo, mantidos={1}, apagados={2}, viewports preservadas={3}, viewports apagadas={4}.",
                sheet.Sequencia,
                kept,
                erased,
                viewportsKept,
                viewportsErased));
        }

        private void FitLayoutViewToSheet(
            BlockTableRecord paperSpace,
            Transaction transaction,
            FolhaInfo sheet,
            Action<string> progress)
        {
            Point2d sheetCenter = new Point2d(
                (sheet.Limites.MinPoint.X + sheet.Limites.MaxPoint.X) / 2.0,
                (sheet.Limites.MinPoint.Y + sheet.Limites.MaxPoint.Y) / 2.0);

            int adjusted = 0;
            foreach (ObjectId entityId in paperSpace)
            {
                var viewport = transaction.GetObject(entityId, OpenMode.ForRead, false) as Viewport;
                if (viewport == null || !IsBasePaperViewport(viewport)) continue;

                if (!viewport.IsWriteEnabled) viewport.UpgradeOpen();
                viewport.ViewCenter = sheetCenter;
                viewport.ViewHeight = CalculatePaperViewHeight(viewport, sheet);
                adjusted++;
            }

            Report(progress, string.Format(
                "DWG folha {0}: vista do Layout ajustada ({1} viewport base).",
                sheet.Sequencia,
                adjusted));
        }

        private static Layout GetLayout(Database database, Transaction transaction, string layoutName)
        {
            var layouts = (DBDictionary)transaction.GetObject(database.LayoutDictionaryId, OpenMode.ForRead);
            if (!layouts.Contains(layoutName))
            {
                throw new InvalidOperationException("Layout não encontrado no DWG: " + layoutName);
            }

            return (Layout)transaction.GetObject(layouts.GetAt(layoutName), OpenMode.ForRead);
        }

        private bool IsSheetBlock(BlockReference block, Transaction transaction)
        {
            FolhaFormat ignored;
            return _formats.TryParse(GetEffectiveBlockName(block, transaction), out ignored);
        }

        private static bool IsSelectedSheetBlock(
            BlockReference block,
            Transaction transaction,
            FolhaInfo sheet)
        {
            if (!string.Equals(
                GetEffectiveBlockName(block, transaction),
                sheet.BlockName,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Point3d position = block.Position;
            if (Math.Abs(position.X - sheet.Limites.MinPoint.X) <= SheetTolerance &&
                Math.Abs(position.Y - sheet.Limites.MinPoint.Y) <= SheetTolerance)
            {
                return true;
            }

            return EntityIntersectsSheet(block, sheet);
        }

        private static bool IsBasePaperViewport(Viewport viewport)
        {
            return viewport.Number <= 1;
        }

        private static bool ViewportBelongsToSheet(Viewport viewport, FolhaInfo sheet)
        {
            Extents2d extents;
            if (TryGetEntityExtents2d(viewport, out extents) && HitsSheet(extents, sheet.Limites))
            {
                return true;
            }

            if (TryGetViewportPaperExtents2d(viewport, out extents) && HitsSheet(extents, sheet.Limites))
            {
                return true;
            }

            try
            {
                Point3d center = viewport.CenterPoint;
                return ContainsPoint(
                    sheet.Limites,
                    new Point2d(center.X, center.Y),
                    ViewportSelectionTolerance);
            }
            catch
            {
                return false;
            }
        }

        private static bool EntityIntersectsSheet(Entity entity, FolhaInfo sheet)
        {
            Extents2d extents;
            return TryGetEntityExtents2d(entity, out extents) &&
                   Intersects(extents, sheet.Limites, SheetTolerance);
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

        private static bool TryGetViewportPaperExtents2d(Viewport viewport, out Extents2d extents)
        {
            extents = default(Extents2d);
            if (viewport == null || viewport.Width <= 0.0 || viewport.Height <= 0.0) return false;

            try
            {
                Point3d center = viewport.CenterPoint;
                double halfWidth = Math.Abs(viewport.Width) / 2.0;
                double halfHeight = Math.Abs(viewport.Height) / 2.0;
                extents = new Extents2d(
                    new Point2d(center.X - halfWidth, center.Y - halfHeight),
                    new Point2d(center.X + halfWidth, center.Y + halfHeight));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool HitsSheet(Extents2d viewportExtents, Extents2d sheetExtents)
        {
            return Intersects(viewportExtents, sheetExtents, ViewportSelectionTolerance) &&
                   OverlapArea(viewportExtents, sheetExtents) > 0.0;
        }

        private static bool Intersects(Extents2d first, Extents2d second, double tolerance)
        {
            return first.MaxPoint.X >= second.MinPoint.X - tolerance &&
                   first.MinPoint.X <= second.MaxPoint.X + tolerance &&
                   first.MaxPoint.Y >= second.MinPoint.Y - tolerance &&
                   first.MinPoint.Y <= second.MaxPoint.Y + tolerance;
        }

        private static double OverlapArea(Extents2d first, Extents2d second)
        {
            double width = Math.Min(first.MaxPoint.X, second.MaxPoint.X) -
                           Math.Max(first.MinPoint.X, second.MinPoint.X);
            double height = Math.Min(first.MaxPoint.Y, second.MaxPoint.Y) -
                            Math.Max(first.MinPoint.Y, second.MinPoint.Y);
            return width <= 0.0 || height <= 0.0 ? 0.0 : width * height;
        }

        private static bool ContainsPoint(Extents2d extents, Point2d point, double tolerance)
        {
            return point.X >= extents.MinPoint.X - tolerance &&
                   point.X <= extents.MaxPoint.X + tolerance &&
                   point.Y >= extents.MinPoint.Y - tolerance &&
                   point.Y <= extents.MaxPoint.Y + tolerance;
        }

        private static double CalculatePaperViewHeight(Viewport viewport, FolhaInfo sheet)
        {
            double sheetWidth = Math.Max(sheet.Largura, 1.0);
            double sheetHeight = Math.Max(sheet.Altura, 1.0);
            double viewHeight = sheetHeight;

            if (viewport != null && viewport.Width > 0.0 && viewport.Height > 0.0)
            {
                double aspect = Math.Abs(viewport.Width / viewport.Height);
                if (aspect > 0.0) viewHeight = Math.Max(sheetHeight, sheetWidth / aspect);
            }

            return viewHeight * LayoutViewMarginFactor;
        }

        private static string GetEffectiveBlockName(BlockReference block, Transaction transaction)
        {
            ObjectId definitionId = block.IsDynamicBlock
                ? block.DynamicBlockTableRecord
                : block.BlockTableRecord;
            var definition = (BlockTableRecord)transaction.GetObject(definitionId, OpenMode.ForRead);
            return definition.Name;
        }

        private static void EnsureOutputDoesNotReplaceSource(string sourcePath, string outputPath)
        {
            if (string.Equals(
                Path.GetFullPath(sourcePath),
                Path.GetFullPath(outputPath),
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "O DWG de saída não pode sobrescrever o desenho aberto: " + outputPath);
            }
        }

        private static void DeleteExistingOutput(string outputPath, bool overwriteExisting)
        {
            if (!File.Exists(outputPath)) return;
            if (!overwriteExisting) throw new IOException("Arquivo já existe: " + outputPath);
            File.Delete(outputPath);
        }

        private static void Erase(Entity entity)
        {
            if (entity == null || entity.IsErased) return;
            if (!entity.IsWriteEnabled) entity.UpgradeOpen();
            entity.Erase();
        }

        private void Report(Action<string> progress, string message)
        {
            _zwcad.WriteMessage(message);
            progress?.Invoke(message);
        }
    }
}
