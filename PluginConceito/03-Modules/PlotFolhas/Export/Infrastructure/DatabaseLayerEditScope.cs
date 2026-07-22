using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class DatabaseLayerEditScope : IDisposable
    {
        private readonly Transaction _transaction;
        private readonly Dictionary<ObjectId, LayerState> _states =
            new Dictionary<ObjectId, LayerState>();
        private bool _restored;

        public DatabaseLayerEditScope(Database database, Transaction transaction)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));

            CaptureAndMakeEditable(database);
        }

        public bool WasOriginallyVisible(ObjectId layerId)
        {
            LayerState state;
            return layerId.IsNull ||
                !_states.TryGetValue(layerId, out state) ||
                (!state.IsOff && !state.IsFrozen);
        }

        public void Restore()
        {
            if (_restored) return;

            foreach (KeyValuePair<ObjectId, LayerState> entry in _states)
            {
                var layer = (LayerTableRecord)_transaction.GetObject(
                    entry.Key,
                    OpenMode.ForWrite,
                    false);
                layer.IsOff = entry.Value.IsOff;
                layer.IsFrozen = entry.Value.IsFrozen;
                layer.IsLocked = entry.Value.IsLocked;
            }

            _restored = true;
        }

        public void Dispose()
        {
            Restore();
        }

        private void CaptureAndMakeEditable(Database database)
        {
            var layerTable = (LayerTable)_transaction.GetObject(
                database.LayerTableId,
                OpenMode.ForRead);

            foreach (ObjectId layerId in layerTable)
            {
                var layer = (LayerTableRecord)_transaction.GetObject(
                    layerId,
                    OpenMode.ForWrite,
                    false);
                _states.Add(
                    layerId,
                    new LayerState(layer.IsOff, layer.IsFrozen, layer.IsLocked));

                if (layer.IsLocked) layer.IsLocked = false;
                if (layer.IsFrozen) layer.IsFrozen = false;
            }
        }

        private sealed class LayerState
        {
            public LayerState(bool isOff, bool isFrozen, bool isLocked)
            {
                IsOff = isOff;
                IsFrozen = isFrozen;
                IsLocked = isLocked;
            }

            public bool IsOff { get; private set; }
            public bool IsFrozen { get; private set; }
            public bool IsLocked { get; private set; }
        }
    }
}
