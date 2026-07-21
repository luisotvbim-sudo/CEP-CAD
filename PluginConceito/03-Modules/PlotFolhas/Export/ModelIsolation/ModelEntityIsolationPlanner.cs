using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ModelEntityIsolationPlanner
    {
        public ModelIsolationPlan Create(
            Transaction transaction,
            IEnumerable<ObjectId> entityIds,
            IReadOnlyList<ViewportModelRegion> regions)
        {
            var plan = new ModelIsolationPlan();

            foreach (ObjectId entityId in entityIds)
            {
                Entity entity = CadEntityAccess.OpenEntityOrNull(transaction, entityId);
                if (entity == null || entity.IsErased) continue;

                EntityRegionMatch match = Match(entity, regions);
                if (!match.HasExtents)
                {
                    plan.KeepWithoutExtents();
                }
                else if (match.Intersects)
                {
                    plan.KeepViewportMatch();
                }
                else
                {
                    plan.Erase(entityId);
                }
            }

            return plan;
        }

        private static EntityRegionMatch Match(
            Entity entity,
            IEnumerable<ViewportModelRegion> regions)
        {
            bool hasExtents = false;

            foreach (ViewportModelRegion region in regions)
            {
                bool intersects;
                bool regionHasExtents = region.TryIntersects(entity, out intersects);
                hasExtents = hasExtents || regionHasExtents;

                if (regionHasExtents && intersects)
                    return new EntityRegionMatch(true, true);
            }

            return new EntityRegionMatch(hasExtents, false);
        }

        private struct EntityRegionMatch
        {
            public EntityRegionMatch(bool hasExtents, bool intersects)
            {
                HasExtents = hasExtents;
                Intersects = intersects;
            }

            public bool HasExtents { get; }

            public bool Intersects { get; }
        }
    }
}
