using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ViewportModelRegionProvider
    {
        private readonly PaperSpaceViewportCatalog _viewportCatalog =
            new PaperSpaceViewportCatalog();
        private readonly ViewportModelRegionFactory _regionFactory =
            new ViewportModelRegionFactory();

        public IReadOnlyList<ViewportModelRegion> Create(
            Database database,
            string layoutName,
            Transaction transaction)
        {
            IReadOnlyList<PaperSpaceViewport> viewports = _viewportCatalog.Find(
                database,
                layoutName,
                transaction);
            ObjectId baseViewportId = FindBaseViewportId(viewports);
            var regions = new List<ViewportModelRegion>();

            foreach (PaperSpaceViewport entry in viewports)
            {
                Viewport viewport = entry.Viewport;
                if (entry.Id == baseViewportId || !viewport.On) continue;

                RejectPerspectiveViewport(viewport);

                try
                {
                    regions.Add(_regionFactory.Create(viewport, transaction));
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        "Não foi possível calcular a região da viewport " + viewport.Number +
                        ". O Model não foi alterado.",
                        exception);
                }
            }

            return regions;
        }

        private static ObjectId FindBaseViewportId(
            IEnumerable<PaperSpaceViewport> viewports)
        {
            foreach (PaperSpaceViewport entry in viewports)
            {
                if (entry.Viewport.Number == 1) return entry.Id;
            }

            return ObjectId.Null;
        }

        private static void RejectPerspectiveViewport(Viewport viewport)
        {
            if (!viewport.PerspectiveOn) return;

            throw new InvalidOperationException(
                "A folha contém uma viewport em perspectiva. " +
                "O Model não foi alterado porque esse tipo de vista ainda não pode ser isolado com segurança.");
        }
    }
}
