using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ViewportModelIsolator
    {
        private readonly ViewportModelRegionProvider _regionProvider =
            new ViewportModelRegionProvider();
        private readonly ModelEntityIsolationPlanner _planner =
            new ModelEntityIsolationPlanner();
        private readonly ModelSpaceEditor _modelSpaceEditor =
            new ModelSpaceEditor();

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
                IReadOnlyList<ObjectId> entityIds = ModelSpaceEntityCatalog.Snapshot(
                    database,
                    transaction);

                ModelIsolationResult result = IsolateEntities(
                    transaction,
                    entityIds,
                    regions);
                transaction.Commit();
                return result;
            }
        }

        private ModelIsolationResult IsolateEntities(
            Transaction transaction,
            IReadOnlyList<ObjectId> entityIds,
            IReadOnlyList<ViewportModelRegion> regions)
        {
            if (regions.Count == 0)
                return _modelSpaceEditor.Clear(transaction, entityIds);

            ModelIsolationPlan plan = _planner.Create(transaction, entityIds, regions);
            if (plan.RequiresSafetyPreservation)
            {
                return _modelSpaceEditor.Preserve(
                    transaction,
                    entityIds,
                    regions.Count,
                    plan.EntitiesKeptWithoutExtents);
            }

            return _modelSpaceEditor.Apply(
                transaction,
                plan,
                regions.Count);
        }
    }
}
