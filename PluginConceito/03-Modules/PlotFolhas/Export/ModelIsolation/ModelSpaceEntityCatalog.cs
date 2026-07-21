using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal static class ModelSpaceEntityCatalog
    {
        public static IReadOnlyList<ObjectId> Snapshot(
            Database database,
            Transaction transaction)
        {
            var blockTable = (BlockTable)transaction.GetObject(
                database.BlockTableId,
                OpenMode.ForRead);
            var modelSpace = (BlockTableRecord)transaction.GetObject(
                blockTable[BlockTableRecord.ModelSpace],
                OpenMode.ForRead);
            var entityIds = new List<ObjectId>();

            foreach (ObjectId entityId in modelSpace)
                entityIds.Add(entityId);

            return entityIds;
        }
    }
}
