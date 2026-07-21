using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PaperSpaceViewport
    {
        public PaperSpaceViewport(ObjectId id, Viewport viewport)
        {
            Id = id;
            Viewport = viewport;
        }

        public ObjectId Id { get; }

        public Viewport Viewport { get; }
    }
}
