using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ViewportModelIsolator
    {
        private readonly ViewportModelRegionProvider _regionProvider =
            new ViewportModelRegionProvider();

        public ModelIsolationResult Isolate(Database database, string layoutName)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (string.IsNullOrWhiteSpace(layoutName))
                throw new ArgumentException("Layout obrigatório.", nameof(layoutName));

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                IReadOnlyList<ViewportModelRegion> regions = _regionProvider.Create(
                    database,
                    layoutName,
                    transaction);

                BlockTableRecord modelSpace = OpenModelSpace(database, transaction);
                List<ObjectId> entityIds = SnapshotEntityIds(modelSpace);
                var eraseIds = new List<ObjectId>();
                var result = new ModelIsolationResult
                {
                    Outcome = ModelIsolationOutcome.Isolated,
                    ViewportsConsidered = regions.Count
                };

                if (regions.Count == 0)
                {
                    EraseEntireModel(transaction, entityIds, result);
                    transaction.Commit();
                    return result;
                }

                int entitiesMatchedByViewport = 0;

                foreach (ObjectId entityId in entityIds)
                {
                    Entity entity = OpenEntityOrNull(transaction, entityId);
                    if (entity == null || entity.IsErased) continue;

                    bool hasExtents = false;
                    bool intersects = false;

                    foreach (ViewportModelRegion region in regions)
                    {
                        bool regionIntersects;
                        bool regionHasExtents = region.TryIntersects(entity, out regionIntersects);
                        hasExtents = hasExtents || regionHasExtents;

                        if (regionHasExtents && regionIntersects)
                        {
                            intersects = true;
                            entitiesMatchedByViewport++;
                            break;
                        }
                    }

                    if (!hasExtents)
                    {
                        result.EntitiesKept++;
                        result.EntitiesKeptWithoutExtents++;
                    }
                    else if (intersects)
                    {
                        result.EntitiesKept++;
                    }
                    else
                    {
                        eraseIds.Add(entityId);
                    }
                }

                if (eraseIds.Count > 0 && entitiesMatchedByViewport == 0)
                {
                    PreserveEntireModel(
                        transaction,
                        entityIds,
                        result);
                    transaction.Commit();
                    return result;
                }

                foreach (ObjectId entityId in eraseIds)
                {
                    Entity entity = OpenEntityOrNull(transaction, entityId);
                    if (entity == null || entity.IsErased) continue;

                    if (!entity.IsWriteEnabled) entity.UpgradeOpen();
                    entity.Erase();
                    result.EntitiesErased++;
                }

                transaction.Commit();
                return result;
            }
        }

        private static void PreserveEntireModel(
            Transaction transaction,
            IEnumerable<ObjectId> entityIds,
            ModelIsolationResult result)
        {
            int kept = 0;
            foreach (ObjectId entityId in entityIds)
            {
                Entity entity = OpenEntityOrNull(transaction, entityId);
                if (entity != null && !entity.IsErased) kept++;
            }

            result.EntitiesKept = kept;
            result.EntitiesErased = 0;
            result.Outcome = ModelIsolationOutcome.ModelPreservedWithoutMatches;
        }

        private static void EraseEntireModel(
            Transaction transaction,
            IEnumerable<ObjectId> entityIds,
            ModelIsolationResult result)
        {
            foreach (ObjectId entityId in entityIds)
            {
                Entity entity = OpenEntityOrNull(transaction, entityId);
                if (entity == null || entity.IsErased) continue;

                if (!entity.IsWriteEnabled) entity.UpgradeOpen();
                entity.Erase();
                result.EntitiesErased++;
            }

            result.EntitiesKept = 0;
            result.Outcome = ModelIsolationOutcome.ModelClearedWithoutViewport;
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

        private static List<ObjectId> SnapshotEntityIds(BlockTableRecord modelSpace)
        {
            var entityIds = new List<ObjectId>();
            foreach (ObjectId entityId in modelSpace) entityIds.Add(entityId);
            return entityIds;
        }

        private static Entity OpenEntityOrNull(Transaction transaction, ObjectId entityId)
        {
            if (entityId.IsNull) return null;

            try
            {
                return transaction.GetObject(entityId, OpenMode.ForRead, false) as Entity;
            }
            catch
            {
                return null;
            }
        }
    }
}
