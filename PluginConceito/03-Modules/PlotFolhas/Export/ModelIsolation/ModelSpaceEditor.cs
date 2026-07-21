using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ModelSpaceEditor
    {
        public ModelIsolationResult Clear(
            Transaction transaction,
            IEnumerable<ObjectId> entityIds)
        {
            var result = CreateResult(ModelIsolationOutcome.ModelClearedWithoutViewport, 0);
            result.EntitiesErased = Erase(transaction, entityIds);
            return result;
        }

        public ModelIsolationResult Preserve(
            Transaction transaction,
            IEnumerable<ObjectId> entityIds,
            int viewportCount,
            int entitiesWithoutExtents)
        {
            var result = CreateResult(
                ModelIsolationOutcome.ModelPreservedWithoutMatches,
                viewportCount);
            result.EntitiesKept = CountActive(transaction, entityIds);
            result.EntitiesKeptWithoutExtents = entitiesWithoutExtents;
            return result;
        }

        public ModelIsolationResult Apply(
            Transaction transaction,
            ModelIsolationPlan plan,
            int viewportCount)
        {
            var result = CreateResult(ModelIsolationOutcome.Isolated, viewportCount);
            result.EntitiesKept = plan.EntitiesKept;
            result.EntitiesKeptWithoutExtents = plan.EntitiesKeptWithoutExtents;
            result.EntitiesErased = Erase(transaction, plan.EntityIdsToErase);
            return result;
        }

        private static int Erase(
            Transaction transaction,
            IEnumerable<ObjectId> entityIds)
        {
            int count = 0;

            foreach (ObjectId entityId in entityIds)
            {
                Entity entity = CadEntityAccess.OpenEntityOrNull(transaction, entityId);
                if (entity == null || entity.IsErased) continue;

                CadEntityAccess.Erase(entity);
                count++;
            }

            return count;
        }

        private static int CountActive(
            Transaction transaction,
            IEnumerable<ObjectId> entityIds)
        {
            int count = 0;

            foreach (ObjectId entityId in entityIds)
            {
                Entity entity = CadEntityAccess.OpenEntityOrNull(transaction, entityId);
                if (entity != null && !entity.IsErased) count++;
            }

            return count;
        }

        private static ModelIsolationResult CreateResult(
            ModelIsolationOutcome outcome,
            int viewportCount)
        {
            return new ModelIsolationResult
            {
                Outcome = outcome,
                ViewportsConsidered = viewportCount
            };
        }
    }
}
