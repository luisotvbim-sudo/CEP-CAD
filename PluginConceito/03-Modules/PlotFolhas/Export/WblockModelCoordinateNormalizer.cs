using System;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ModelCoordinateSnapshot
    {
        public ModelCoordinateSnapshot(
            int entityCount,
            int entitiesWithExtents,
            int entitiesWithoutExtents,
            bool hasAggregateExtents,
            Point3d minimum,
            Point3d maximum,
            Point3d insbase)
        {
            EntityCount = entityCount;
            EntitiesWithExtents = entitiesWithExtents;
            EntitiesWithoutExtents = entitiesWithoutExtents;
            HasAggregateExtents = hasAggregateExtents;
            Minimum = minimum;
            Maximum = maximum;
            Insbase = insbase;
        }

        public int EntityCount { get; }
        public int EntitiesWithExtents { get; }
        public int EntitiesWithoutExtents { get; }
        public bool HasAggregateExtents { get; }
        public Point3d Minimum { get; }
        public Point3d Maximum { get; }
        public Point3d Insbase { get; }
    }

    internal sealed class WblockModelCoordinateNormalizer
    {
        private const double Tolerance = 1e-9;

        public ModelCoordinateSnapshot Capture(Database database)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));

            using (Transaction transaction =
                database.TransactionManager.StartTransaction())
            {
                BlockTableRecord modelSpace =
                    OpenModelSpace(database, transaction, OpenMode.ForRead);
                int entityCount = 0;
                int withExtents = 0;
                int withoutExtents = 0;
                bool hasAggregateExtents = false;
                Point3d minimum = Point3d.Origin;
                Point3d maximum = Point3d.Origin;

                foreach (ObjectId entityId in modelSpace)
                {
                    var entity = transaction.GetObject(
                        entityId,
                        OpenMode.ForRead,
                        false) as Entity;
                    if (entity == null || entity.IsErased) continue;

                    entityCount++;
                    try
                    {
                        Extents3d extents = entity.GeometricExtents;
                        withExtents++;
                        if (!hasAggregateExtents)
                        {
                            minimum = extents.MinPoint;
                            maximum = extents.MaxPoint;
                            hasAggregateExtents = true;
                        }
                        else
                        {
                            minimum = Minimum(minimum, extents.MinPoint);
                            maximum = Maximum(maximum, extents.MaxPoint);
                        }
                    }
                    catch
                    {
                        withoutExtents++;
                    }
                }

                return new ModelCoordinateSnapshot(
                    entityCount,
                    withExtents,
                    withoutExtents,
                    hasAggregateExtents,
                    minimum,
                    maximum,
                    database.Insbase);
            }
        }

        public void Normalize(
            Database clone,
            ModelCoordinateSnapshot source)
        {
            if (clone == null) throw new ArgumentNullException(nameof(clone));
            if (source == null) throw new ArgumentNullException(nameof(source));

            ModelCoordinateSnapshot cloned = Capture(clone);
            ValidateComposition(source, cloned);
            Vector3d translation = ResolveUniformTranslation(source, cloned);

            if (!IsZero(translation))
                TransformModelSpace(clone, translation.Negate());

            clone.Insbase = source.Insbase;
            ValidateNormalized(source, Capture(clone));
        }

        private static void ValidateComposition(
            ModelCoordinateSnapshot source,
            ModelCoordinateSnapshot cloned)
        {
            if (source.EntityCount != cloned.EntityCount ||
                source.EntitiesWithExtents != cloned.EntitiesWithExtents ||
                source.EntitiesWithoutExtents != cloned.EntitiesWithoutExtents)
            {
                throw new InvalidOperationException(
                    "O Wblock alterou a composição do Model.");
            }
        }

        private static Vector3d ResolveUniformTranslation(
            ModelCoordinateSnapshot source,
            ModelCoordinateSnapshot cloned)
        {
            if (!source.HasAggregateExtents || !cloned.HasAggregateExtents)
            {
                if (source.EntityCount == 0)
                    return new Vector3d(0.0, 0.0, 0.0);

                throw new InvalidOperationException(
                    "Não foi possível validar as coordenadas do Model após o Wblock.");
            }

            Vector3d minimumTranslation =
                cloned.Minimum - source.Minimum;
            Vector3d maximumTranslation =
                cloned.Maximum - source.Maximum;
            if (!AreEqual(minimumTranslation, maximumTranslation))
            {
                throw new InvalidOperationException(
                    "O Wblock alterou os limites do Model de forma não uniforme.");
            }

            return minimumTranslation;
        }

        private static void TransformModelSpace(
            Database database,
            Vector3d correction)
        {
            Matrix3d transform = Matrix3d.Displacement(correction);
            using (Transaction transaction =
                database.TransactionManager.StartTransaction())
            using (var layerEditScope = new DatabaseLayerEditScope(
                database,
                transaction))
            {
                BlockTableRecord modelSpace =
                    OpenModelSpace(database, transaction, OpenMode.ForRead);
                foreach (ObjectId entityId in modelSpace)
                {
                    var entity = transaction.GetObject(
                        entityId,
                        OpenMode.ForWrite,
                        false) as Entity;
                    if (entity == null || entity.IsErased) continue;

                    entity.TransformBy(transform);
                }

                layerEditScope.Restore();
                transaction.Commit();
            }
        }

        private static void ValidateNormalized(
            ModelCoordinateSnapshot source,
            ModelCoordinateSnapshot normalized)
        {
            ValidateComposition(source, normalized);
            if (source.HasAggregateExtents != normalized.HasAggregateExtents ||
                source.HasAggregateExtents &&
                (!AreEqual(source.Minimum, normalized.Minimum) ||
                    !AreEqual(source.Maximum, normalized.Maximum)) ||
                !AreEqual(source.Insbase, normalized.Insbase))
            {
                throw new InvalidOperationException(
                    "A validação das coordenadas do Model após o Wblock falhou.");
            }
        }

        private static BlockTableRecord OpenModelSpace(
            Database database,
            Transaction transaction,
            OpenMode openMode)
        {
            var blockTable = (BlockTable)transaction.GetObject(
                database.BlockTableId,
                OpenMode.ForRead);
            return (BlockTableRecord)transaction.GetObject(
                blockTable[BlockTableRecord.ModelSpace],
                openMode);
        }

        private static Point3d Minimum(Point3d first, Point3d second)
        {
            return new Point3d(
                Math.Min(first.X, second.X),
                Math.Min(first.Y, second.Y),
                Math.Min(first.Z, second.Z));
        }

        private static Point3d Maximum(Point3d first, Point3d second)
        {
            return new Point3d(
                Math.Max(first.X, second.X),
                Math.Max(first.Y, second.Y),
                Math.Max(first.Z, second.Z));
        }

        private static bool IsZero(Vector3d vector)
        {
            return AreEqual(vector, new Vector3d(0.0, 0.0, 0.0));
        }

        private static bool AreEqual(Vector3d first, Vector3d second)
        {
            return AreEqual(first.X, second.X) &&
                AreEqual(first.Y, second.Y) &&
                AreEqual(first.Z, second.Z);
        }

        private static bool AreEqual(Point3d first, Point3d second)
        {
            return AreEqual(first.X, second.X) &&
                AreEqual(first.Y, second.Y) &&
                AreEqual(first.Z, second.Z);
        }

        private static bool AreEqual(double first, double second)
        {
            double scale = Math.Max(
                1.0,
                Math.Max(Math.Abs(first), Math.Abs(second)));
            return Math.Abs(first - second) <= scale * Tolerance;
        }
    }
}
