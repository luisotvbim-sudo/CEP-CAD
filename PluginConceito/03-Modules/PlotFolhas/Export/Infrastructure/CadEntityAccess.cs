using System;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal static class CadEntityAccess
    {
        public static Layout OpenLayout(
            Database database,
            string layoutName,
            Transaction transaction)
        {
            var layouts = (DBDictionary)transaction.GetObject(
                database.LayoutDictionaryId,
                OpenMode.ForRead);
            if (!layouts.Contains(layoutName))
                throw new InvalidOperationException("Layout não encontrado no DWG: " + layoutName);

            return (Layout)transaction.GetObject(layouts.GetAt(layoutName), OpenMode.ForRead);
        }

        public static Entity OpenEntityOrNull(Transaction transaction, ObjectId entityId)
        {
            if (entityId.IsNull) return null;

            try
            {
                return transaction.GetObject(
                    entityId,
                    OpenMode.ForRead,
                    false,
                    true) as Entity;
            }
            catch
            {
                return null;
            }
        }

        public static bool TryGetExtents2d(Entity entity, out Extents2d extents)
        {
            extents = default(Extents2d);
            if (entity == null) return false;

            try
            {
                Extents3d source = entity.GeometricExtents;
                extents = new Extents2d(
                    new Point2d(source.MinPoint.X, source.MinPoint.Y),
                    new Point2d(source.MaxPoint.X, source.MaxPoint.Y));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Erase(Entity entity)
        {
            if (!entity.IsWriteEnabled) entity.UpgradeOpen();
            entity.Erase();
        }
    }
}
