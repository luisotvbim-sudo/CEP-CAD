using System;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class StampBlockLocator
    {
        public BlockReference FindBestMatch(
            BlockTableRecord paperSpace,
            Transaction transaction,
            string blockName,
            Extents2d sheetBoundary)
        {
            BlockReference bestMatch = null;
            double bestOverlapArea = 0;

            foreach (ObjectId entityId in paperSpace)
            {
                BlockReference block = OpenBlockOrNull(transaction, entityId);
                if (!HasName(block, transaction, blockName)) continue;

                double overlapArea = CalculateOverlapArea(block, sheetBoundary);
                if (overlapArea <= bestOverlapArea) continue;

                bestOverlapArea = overlapArea;
                bestMatch = block;
            }

            return bestMatch;
        }

        private static bool HasName(
            BlockReference block,
            Transaction transaction,
            string expectedName)
        {
            return block != null && string.Equals(
                BlockNameHelper.GetEffectiveName(block, transaction),
                expectedName,
                StringComparison.OrdinalIgnoreCase);
        }

        private static double CalculateOverlapArea(
            BlockReference block,
            Extents2d sheetBoundary)
        {
            try
            {
                Extents3d extents = block.GeometricExtents;
                double width = Math.Min(sheetBoundary.MaxPoint.X, extents.MaxPoint.X) -
                    Math.Max(sheetBoundary.MinPoint.X, extents.MinPoint.X);
                double height = Math.Min(sheetBoundary.MaxPoint.Y, extents.MaxPoint.Y) -
                    Math.Max(sheetBoundary.MinPoint.Y, extents.MinPoint.Y);

                return width > 0 && height > 0 ? width * height : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static BlockReference OpenBlockOrNull(
            Transaction transaction,
            ObjectId entityId)
        {
            if (entityId.IsNull || entityId.IsErased) return null;

            try
            {
                return transaction.GetObject(
                    entityId,
                    OpenMode.ForRead,
                    false) as BlockReference;
            }
            catch
            {
                return null;
            }
        }
    }
}
