using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal static class BlockNameHelper
    {
        public static ObjectId GetEffectiveDefinitionId(BlockReference block)
        {
            return block.IsDynamicBlock
                ? block.DynamicBlockTableRecord
                : block.BlockTableRecord;
        }

        public static string GetEffectiveName(BlockReference block, Transaction transaction)
        {
            ObjectId definitionId = GetEffectiveDefinitionId(block);
            BlockTableRecord definition = (BlockTableRecord)transaction.GetObject(
                definitionId, OpenMode.ForRead);
            return definition.Name;
        }
    }
}
