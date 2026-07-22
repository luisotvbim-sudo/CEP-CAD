using System;
using System.Collections.Generic;
using System.Linq;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ModelEntityIsolator
    {
        private readonly ModelCurveClipper _curveClipper;

        public ModelEntityIsolator()
        {
            _curveClipper = new ModelCurveClipper();
        }

        public void Isolate(
            BlockTableRecord modelSpace,
            Transaction transaction,
            IReadOnlyList<ViewportModelRegion> regions,
            ModelIsolationResult result,
            Action<string> report,
            DatabaseLayerEditScope layerStates)
        {
            List<ObjectId> entityIds = modelSpace.Cast<ObjectId>().ToList();
            foreach (ObjectId entityId in entityIds)
            {
                var entity = transaction.GetObject(
                    entityId,
                    OpenMode.ForRead,
                    false) as Entity;
                if (entity == null || entity.IsErased) continue;

                IsolateEntity(
                    entity,
                    modelSpace,
                    transaction,
                    regions,
                    result,
                    report,
                    layerStates);
            }
        }

        private void IsolateEntity(
            Entity entity,
            BlockTableRecord modelSpace,
            Transaction transaction,
            IReadOnlyList<ViewportModelRegion> regions,
            ModelIsolationResult result,
            Action<string> report,
            DatabaseLayerEditScope layerStates)
        {
            if (!ModelEntityVisibility.IsGloballyVisible(entity, layerStates))
            {
                EraseInvisibleEntity(entity, result);
                return;
            }

            IReadOnlyList<ViewportModelRegion> visibleRegions = GetRegionsWhereLayerIsVisible(
                entity.LayerId,
                regions);
            if (visibleRegions.Count == 0)
            {
                EraseInvisibleEntity(entity, result);
                return;
            }

            if (!IntersectsAnyRegion(entity, visibleRegions))
            {
                CadEntityAccess.Erase(entity);
                result.EntitiesErased++;
                return;
            }

            // Blocos e Xrefs permanecem inteiros quando qualquer parte aparece.
            if (entity is BlockReference)
            {
                result.EntitiesKept++;
                result.BlockReferencesKept++;
                return;
            }

            var curve = entity as Curve;
            if (curve != null)
            {
                _curveClipper.Clip(
                    curve,
                    modelSpace,
                    transaction,
                    visibleRegions,
                    result,
                    report);
                return;
            }

            // Texto, hatch, cota, imagem, proxy etc. permanecem inteiros.
            result.EntitiesKept++;
        }

        private static IReadOnlyList<ViewportModelRegion> GetRegionsWhereLayerIsVisible(
            ObjectId layerId,
            IEnumerable<ViewportModelRegion> regions)
        {
            var result = new List<ViewportModelRegion>();
            foreach (ViewportModelRegion region in regions)
            {
                if (region.IsLayerVisible(layerId)) result.Add(region);
            }

            return result;
        }

        private static void EraseInvisibleEntity(
            Entity entity,
            ModelIsolationResult result)
        {
            CadEntityAccess.Erase(entity);
            result.EntitiesErased++;
            result.EntitiesErasedByVisibility++;
        }

        private static bool IntersectsAnyRegion(
            Entity entity,
            IEnumerable<ViewportModelRegion> regions)
        {
            foreach (ViewportModelRegion region in regions)
            {
                if (region.Intersects(entity)) return true;
            }

            return false;
        }
    }
}
