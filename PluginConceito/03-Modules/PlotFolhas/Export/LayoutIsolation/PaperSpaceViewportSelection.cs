using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PaperSpaceViewportSelection
    {
        private readonly HashSet<ObjectId> _viewportIds = new HashSet<ObjectId>();
        private readonly HashSet<ObjectId> _clipEntityIds = new HashSet<ObjectId>();

        public PaperSpaceViewportSelection(ObjectId baseViewportId)
        {
            BaseViewportId = baseViewportId;
        }

        public ObjectId BaseViewportId { get; }

        public int ModelViewportCount { get; private set; }

        public bool ContainsViewport(ObjectId entityId)
        {
            return _viewportIds.Contains(entityId);
        }

        public bool ContainsClipEntity(ObjectId entityId)
        {
            return _clipEntityIds.Contains(entityId);
        }

        public void Add(ObjectId entityId, Viewport viewport)
        {
            _viewportIds.Add(entityId);
            AddClipEntity(viewport);

            if (entityId != BaseViewportId && viewport.On && !viewport.PerspectiveOn)
                ModelViewportCount++;
        }

        private void AddClipEntity(Viewport viewport)
        {
            try
            {
                if (viewport.NonRectClipOn && !viewport.NonRectClipEntityId.IsNull)
                    _clipEntityIds.Add(viewport.NonRectClipEntityId);
            }
            catch
            {
                // O limite retangular será usado se o clip estiver inválido.
            }
        }
    }
}
