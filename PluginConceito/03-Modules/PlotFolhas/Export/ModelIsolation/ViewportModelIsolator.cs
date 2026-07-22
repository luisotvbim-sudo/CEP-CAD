using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ViewportModelIsolator
    {
        private readonly VisibleViewportRegionProvider _regionProvider;
        private readonly ModelEntityIsolator _entityIsolator;

        public ViewportModelIsolator()
        {
            _regionProvider = new VisibleViewportRegionProvider();
            _entityIsolator = new ModelEntityIsolator();
        }

        public ModelIsolationResult Isolate(
            Database database,
            string layoutName,
            Action<string> report = null)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (string.IsNullOrWhiteSpace(layoutName))
                throw new ArgumentException("Layout obrigatorio.", nameof(layoutName));

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                report = report ?? delegate { };
                List<ViewportModelRegion> regions = _regionProvider.Create(
                    database,
                    layoutName,
                    transaction,
                    report);

                try
                {
                    using (var layerEditScope = new DatabaseLayerEditScope(
                        database,
                        transaction))
                    {
                        BlockTableRecord modelSpace = OpenModelSpace(database, transaction);
                        var result = new ModelIsolationResult
                        {
                            LayoutName = layoutName,
                            ViewportsConsidered = regions.Count
                        };

                        _entityIsolator.Isolate(
                            modelSpace,
                            transaction,
                            regions,
                            result,
                            report,
                            layerEditScope);
                        layerEditScope.Restore();
                        transaction.Commit();
                        return result;
                    }
                }
                finally
                {
                    foreach (ViewportModelRegion region in regions) region.Dispose();
                }
            }
        }

        private static BlockTableRecord OpenModelSpace(
            Database database,
            Transaction transaction)
        {
            var blockTable = (BlockTable)transaction.GetObject(
                database.BlockTableId,
                OpenMode.ForRead);
            return (BlockTableRecord)transaction.GetObject(
                blockTable[BlockTableRecord.ModelSpace],
                OpenMode.ForWrite);
        }
    }
}
