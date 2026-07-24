using System;
using System.Collections.Generic;
using System.Linq;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class DwgLayoutIsolator
    {
        private readonly SelectedSheetLocator _sheetLocator;
        private readonly PaperSpaceViewportSelector _viewportSelector;
        private readonly PaperSpaceEntitySelector _entitySelector;
        private readonly DwgOpeningViewService _openingViewService;

        public DwgLayoutIsolator(FolhaFormatCatalog formats)
        {
            if (formats == null) throw new ArgumentNullException(nameof(formats));

            _sheetLocator = new SelectedSheetLocator();
            _viewportSelector = new PaperSpaceViewportSelector();
            _entitySelector = new PaperSpaceEntitySelector(formats);
            _openingViewService = new DwgOpeningViewService();
        }

        public DwgLayoutIsolationResult Isolate(Database database, FolhaInfo sheet)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            using (var sheetRegion = new SheetRegion(sheet.Limites))
            {
                Layout layout = CadEntityAccess.OpenLayout(database, sheet.LayoutName, transaction);
                var paperSpace = (BlockTableRecord)transaction.GetObject(
                    layout.BlockTableRecordId,
                    OpenMode.ForWrite);
                List<ObjectId> entityIds = paperSpace.Cast<ObjectId>().ToList();

                ObjectId selectedSheetId = _sheetLocator.Find(
                    database,
                    entityIds,
                    transaction,
                    sheet,
                    sheetRegion);
                PaperSpaceViewportSelection viewports = _viewportSelector.Select(
                    entityIds,
                    transaction,
                    sheetRegion);

                using (var layerEditScope = new DatabaseLayerEditScope(
                    database,
                    transaction))
                {
                    DwgLayoutIsolationResult result = EraseUnrelatedEntities(
                        entityIds,
                        selectedSheetId,
                        viewports,
                        transaction,
                        sheetRegion);
                    layerEditScope.Restore();
                    transaction.Commit();
                    return result;
                }
            }
        }

        public void PrepareOpeningView(Database database, FolhaInfo sheet)
        {
            _openingViewService.Prepare(database, sheet);
        }

        private DwgLayoutIsolationResult EraseUnrelatedEntities(
            IEnumerable<ObjectId> entityIds,
            ObjectId selectedSheetId,
            PaperSpaceViewportSelection viewports,
            Transaction transaction,
            SheetRegion sheetRegion)
        {
            var result = new DwgLayoutIsolationResult
            {
                ModelViewportsKept = viewports.ModelViewportCount
            };

            foreach (ObjectId entityId in entityIds)
            {
                Entity entity = CadEntityAccess.OpenEntityOrNull(transaction, entityId);
                if (entity == null || entity.IsErased) continue;

                if (_entitySelector.ShouldKeep(
                    entityId,
                    entity,
                    selectedSheetId,
                    viewports,
                    transaction,
                    sheetRegion))
                {
                    result.EntitiesKept++;
                }
                else
                {
                    CadEntityAccess.Erase(entity);
                    result.EntitiesErased++;
                }
            }

            return result;
        }
    }
}
