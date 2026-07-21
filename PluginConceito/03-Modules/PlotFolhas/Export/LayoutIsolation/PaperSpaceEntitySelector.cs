using System;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PaperSpaceEntitySelector
    {
        private readonly FolhaFormatCatalog _formats;

        public PaperSpaceEntitySelector(FolhaFormatCatalog formats)
        {
            _formats = formats ?? throw new ArgumentNullException(nameof(formats));
        }

        public bool ShouldKeep(
            ObjectId entityId,
            Entity entity,
            ObjectId selectedSheetId,
            PaperSpaceViewportSelection viewports,
            Transaction transaction,
            PaperSpaceSheetRegion sheetRegion)
        {
            if (entityId == selectedSheetId || viewports.ContainsClipEntity(entityId)) return true;
            if (entity is Viewport) return viewports.ContainsViewport(entityId);

            var block = entity as BlockReference;
            if (block != null && IsSheetBlock(block, transaction)) return false;

            return sheetRegion.Intersects(entity);
        }

        private bool IsSheetBlock(BlockReference block, Transaction transaction)
        {
            FolhaFormat ignored;
            return _formats.TryParse(BlockNameHelper.GetEffectiveName(block, transaction), out ignored);
        }
    }
}
