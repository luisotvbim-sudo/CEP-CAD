using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ModelIsolationPlan
    {
        private readonly List<ObjectId> _entityIdsToErase = new List<ObjectId>();

        public IReadOnlyList<ObjectId> EntityIdsToErase
        {
            get { return _entityIdsToErase; }
        }

        public int EntitiesKept { get; private set; }

        public int EntitiesKeptWithoutExtents { get; private set; }

        public int EntitiesMatchedByViewport { get; private set; }

        public bool RequiresSafetyPreservation =>
            _entityIdsToErase.Count > 0 && EntitiesMatchedByViewport == 0;

        public void KeepWithoutExtents()
        {
            EntitiesKept++;
            EntitiesKeptWithoutExtents++;
        }

        public void KeepViewportMatch()
        {
            EntitiesKept++;
            EntitiesMatchedByViewport++;
        }

        public void Erase(ObjectId entityId)
        {
            _entityIdsToErase.Add(entityId);
        }
    }
}
