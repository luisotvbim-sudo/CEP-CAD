using System;
using System.Collections.Generic;
using System.Linq;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ModelSpaceSheetIsolator
    {
        private readonly FolhaFormatCatalog _formats;
        private readonly SelectedSheetLocator _sheetLocator;

        public ModelSpaceSheetIsolator(FolhaFormatCatalog formats)
        {
            _formats = formats ?? throw new ArgumentNullException(nameof(formats));
            _sheetLocator = new SelectedSheetLocator();
        }

        public ModelSpaceSheetIsolationResult Isolate(
            Database database,
            FolhaInfo sheet)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));

            using (Transaction transaction =
                database.TransactionManager.StartTransaction())
            using (var sheetRegion = new SheetRegion(sheet.Limites))
            {
                BlockTableRecord modelSpace = OpenModelSpace(
                    database,
                    transaction);
                List<ObjectId> entityIds = modelSpace
                    .Cast<ObjectId>()
                    .ToList();
                ObjectId selectedSheetId = _sheetLocator.Find(
                    database,
                    entityIds,
                    transaction,
                    sheet,
                    sheetRegion);

                using (var layerEditScope = new DatabaseLayerEditScope(
                    database,
                    transaction))
                {
                    ModelSpaceSheetIsolationResult result =
                        EraseUnrelatedEntities(
                            entityIds,
                            selectedSheetId,
                            transaction,
                            sheetRegion);
                    layerEditScope.Restore();
                    transaction.Commit();
                    return result;
                }
            }
        }

        private ModelSpaceSheetIsolationResult EraseUnrelatedEntities(
            IEnumerable<ObjectId> entityIds,
            ObjectId selectedSheetId,
            Transaction transaction,
            SheetRegion sheetRegion)
        {
            var result = new ModelSpaceSheetIsolationResult();

            foreach (ObjectId entityId in entityIds)
            {
                Entity entity = CadEntityAccess.OpenEntityOrNull(
                    transaction,
                    entityId);
                if (entity == null || entity.IsErased)
                {
                    continue;
                }

                if (ShouldKeep(
                    entityId,
                    entity,
                    selectedSheetId,
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

        private bool ShouldKeep(
            ObjectId entityId,
            Entity entity,
            ObjectId selectedSheetId,
            Transaction transaction,
            SheetRegion sheetRegion)
        {
            if (entityId == selectedSheetId)
            {
                return true;
            }

            var block = entity as BlockReference;
            if (block != null && IsSheetBlock(block, transaction))
            {
                return false;
            }

            return sheetRegion.Intersects(entity);
        }

        private bool IsSheetBlock(
            BlockReference block,
            Transaction transaction)
        {
            FolhaFormat ignored;
            return _formats.TryParse(
                BlockNameHelper.GetEffectiveName(block, transaction),
                out ignored);
        }

        private static BlockTableRecord OpenModelSpace(
            Database database,
            Transaction transaction)
        {
            var blockTable = (BlockTable)transaction.GetObject(
                database.BlockTableId,
                OpenMode.ForRead);
            return (BlockTableRecord)transaction.GetObject(
                blockTable[BlockTableRecord.ModelSpace],
                OpenMode.ForWrite);
        }
    }
}
