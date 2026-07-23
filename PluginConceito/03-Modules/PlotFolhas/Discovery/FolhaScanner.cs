using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class FolhaScanner
    {
        private const double RowTolerance = 10.0;

        private readonly IZwcadContext _zwcad;
        private readonly FolhaFormatCatalog _formats;
        private readonly FolhaBoundaryResolver _boundaryResolver;
        private readonly FolhaValidationService _validation;

        public FolhaScanner(
            IZwcadContext zwcad,
            FolhaFormatCatalog formats,
            FolhaBoundaryResolver boundaryResolver,
            FolhaValidationService validation)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
            _formats = formats ?? throw new ArgumentNullException(nameof(formats));
            _boundaryResolver = boundaryResolver ??
                throw new ArgumentNullException(nameof(boundaryResolver));
            _validation = validation ??
                throw new ArgumentNullException(nameof(validation));
        }

        public IReadOnlyList<FolhaInfo> ScanActiveLayout()
        {
            Document document = GetActiveDocument();
            string layoutName = GetActivePaperLayoutName();
            List<FolhaInfo> found = ScanLayout(document, layoutName);
            List<FolhaInfo> ordered = OrderBySheetPosition(found);

            AssignSequenceAndReport(ordered);
            _validation.ValidateOverlaps(ordered);
            return ordered;
        }

        private Document GetActiveDocument()
        {
            Document document = _zwcad.ActiveDocument;
            if (document == null)
            {
                throw new InvalidOperationException(
                    "Não existe desenho ativo.");
            }

            return document;
        }

        private static string GetActivePaperLayoutName()
        {
            string layoutName = LayoutManager.Current.CurrentLayout;
            if (string.Equals(
                layoutName,
                "Model",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Ative uma aba de layout antes de executar o comando.");
            }

            return layoutName;
        }

        private List<FolhaInfo> ScanLayout(
            Document document,
            string layoutName)
        {
            var found = new List<FolhaInfo>();

            using (Transaction transaction =
                document.Database.TransactionManager.StartTransaction())
            {
                ObjectId layoutId =
                    LayoutManager.Current.GetLayoutId(layoutName);
                var layout = (Layout)transaction.GetObject(
                    layoutId,
                    OpenMode.ForRead);
                var paperSpace = (BlockTableRecord)transaction.GetObject(
                    layout.BlockTableRecordId,
                    OpenMode.ForRead);

                foreach (ObjectId entityId in paperSpace)
                {
                    FolhaInfo sheet = TryCreateSheet(
                        entityId,
                        layout,
                        transaction);
                    if (sheet != null)
                    {
                        found.Add(sheet);
                    }
                }

                transaction.Commit();
            }

            return found;
        }

        private FolhaInfo TryCreateSheet(
            ObjectId entityId,
            Layout layout,
            Transaction transaction)
        {
            var block = transaction.GetObject(
                entityId,
                OpenMode.ForRead,
                false) as BlockReference;
            if (block == null)
            {
                return null;
            }

            string blockName = BlockNameHelper.GetEffectiveName(
                block,
                transaction);
            FolhaFormat format;
            if (!_formats.TryParse(blockName, out format))
            {
                return null;
            }

            bool standardizedBoundary;
            Extents2d limits;
            if (!_boundaryResolver.TryResolve(
                block,
                transaction,
                format,
                out limits,
                out standardizedBoundary))
            {
                return null;
            }

            var sheet = new FolhaInfo
            {
                BlockReferenceId = entityId,
                LayoutId = layout.ObjectId,
                LayoutName = layout.LayoutName,
                BlockName = blockName,
                Formato = format.Name,
                Limites = limits,
                LimitePadronizadoEncontrado = standardizedBoundary
            };

            _validation.ValidateSheet(block, sheet, format);
            return sheet;
        }

        private void AssignSequenceAndReport(IList<FolhaInfo> sheets)
        {
            for (int index = 0; index < sheets.Count; index++)
            {
                FolhaInfo sheet = sheets[index];
                sheet.Sequencia = index + 1;
                _zwcad.WriteMessage(Describe(sheet));
            }
        }

        private static string Describe(FolhaInfo sheet)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Folha {0:00}: {1} ({2}) LL={3:0.###},{4:0.###} " +
                "UR={5:0.###},{6:0.###} limite={7}",
                sheet.Sequencia,
                sheet.BlockName,
                sheet.Formato,
                sheet.Limites.MinPoint.X,
                sheet.Limites.MinPoint.Y,
                sheet.Limites.MaxPoint.X,
                sheet.Limites.MaxPoint.Y,
                sheet.LimitePadronizadoEncontrado
                    ? FolhaBoundaryResolver.BoundaryLayerName
                    : "estimado");
        }

        private static List<FolhaInfo> OrderBySheetPosition(
            IEnumerable<FolhaInfo> sheets)
        {
            var remaining = sheets
                .OrderByDescending(sheet => sheet.Limites.MinPoint.Y)
                .ThenBy(sheet => sheet.Limites.MinPoint.X)
                .ToList();
            var ordered = new List<FolhaInfo>(remaining.Count);

            while (remaining.Count > 0)
            {
                double rowY = remaining[0].Limites.MinPoint.Y;
                List<FolhaInfo> row = remaining
                    .Where(sheet =>
                        Math.Abs(sheet.Limites.MinPoint.Y - rowY) <=
                        RowTolerance)
                    .OrderBy(sheet => sheet.Limites.MinPoint.X)
                    .ToList();

                ordered.AddRange(row);
                foreach (FolhaInfo sheet in row)
                {
                    remaining.Remove(sheet);
                }
            }

            return ordered;
        }
    }
}
