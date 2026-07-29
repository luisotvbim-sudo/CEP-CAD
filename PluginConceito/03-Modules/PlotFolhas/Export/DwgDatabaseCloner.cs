using System;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class DwgDatabaseCloner
    {
        private readonly WblockModelCoordinateNormalizer _coordinateNormalizer;

        public DwgDatabaseCloner()
        {
            _coordinateNormalizer = new WblockModelCoordinateNormalizer();
        }

        public Database Clone(
            Document document,
            string layoutName,
            out ObjectId baseViewportId)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            baseViewportId = ObjectId.Null;

            using (DocumentLock documentLock = document.LockDocument())
            {
                PaperSpaceBaseViewportSnapshot sourceBaseViewport = null;
                if (!string.IsNullOrWhiteSpace(layoutName))
                {
                    sourceBaseViewport =
                        PaperSpaceBaseViewportResolver.CaptureSource(
                            document.Database,
                            layoutName);
                }

                ModelCoordinateSnapshot sourceModel =
                    _coordinateNormalizer.Capture(document.Database);

                Database clone = null;
                try
                {
                    clone = document.Database.Wblock();
                    if (clone == null)
                    {
                        throw new InvalidOperationException(
                            "O ZWCAD não conseguiu criar a cópia do desenho ativo.");
                    }

                    _coordinateNormalizer.Normalize(clone, sourceModel);

                    if (sourceBaseViewport != null)
                    {
                        baseViewportId =
                            PaperSpaceBaseViewportResolver.ResolveCloned(
                                clone,
                                layoutName,
                                sourceBaseViewport);
                    }

                    return clone;
                }
                catch
                {
                    if (clone != null) clone.Dispose();
                    throw;
                }
            }
        }
    }
}
