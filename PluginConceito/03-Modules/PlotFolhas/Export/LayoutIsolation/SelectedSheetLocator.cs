using System;
using System.Collections.Generic;
using System.Linq;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class SelectedSheetLocator
    {
        public ObjectId Find(
            Database database,
            IReadOnlyList<ObjectId> entityIds,
            Transaction transaction,
            FolhaInfo sheet,
            PaperSpaceSheetRegion sheetRegion)
        {
            ObjectId selectedSheetId = FindByHandle(database, entityIds, sheet);
            if (!selectedSheetId.IsNull) return selectedSheetId;

            selectedSheetId = FindByNameAndPosition(entityIds, transaction, sheet, sheetRegion);
            if (!selectedSheetId.IsNull) return selectedSheetId;

            throw new InvalidOperationException(
                "O bloco da folha " + sheet.Sequencia + " não foi encontrado na cópia do desenho.");
        }

        private static ObjectId FindByHandle(
            Database database,
            IReadOnlyList<ObjectId> entityIds,
            FolhaInfo sheet)
        {
            try
            {
                ObjectId mappedId;
                return !sheet.BlockReferenceId.IsNull &&
                    database.TryGetObjectId(sheet.BlockReferenceId.Handle, out mappedId) &&
                    entityIds.Contains(mappedId)
                    ? mappedId
                    : ObjectId.Null;
            }
            catch
            {
                return ObjectId.Null;
            }
        }

        private static ObjectId FindByNameAndPosition(
            IEnumerable<ObjectId> entityIds,
            Transaction transaction,
            FolhaInfo sheet,
            PaperSpaceSheetRegion sheetRegion)
        {
            ObjectId bestId = ObjectId.Null;
            double bestOverlap = 0.0;

            foreach (ObjectId entityId in entityIds)
            {
                var block = CadEntityAccess.OpenEntityOrNull(transaction, entityId) as BlockReference;
                if (block == null || !HasExpectedName(block, transaction, sheet.BlockName)) continue;
                if (sheetRegion.Contains(block.Position)) return entityId;

                Extents2d extents;
                if (!CadEntityAccess.TryGetExtents2d(block, out extents)) continue;

                double overlap = Extents2dRelations.OverlapArea(extents, sheet.Limites);
                if (overlap <= bestOverlap) continue;

                bestOverlap = overlap;
                bestId = entityId;
            }

            return bestId;
        }

        private static bool HasExpectedName(
            BlockReference block,
            Transaction transaction,
            string expectedName)
        {
            return string.Equals(
                BlockNameHelper.GetEffectiveName(block, transaction),
                expectedName,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
