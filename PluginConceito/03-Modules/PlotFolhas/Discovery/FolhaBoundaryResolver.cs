using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class FolhaBoundaryResolver
    {
        public const string BoundaryLayerName = "502-CEP-FOR-06";

        public bool TryResolve(
            BlockReference block,
            Transaction transaction,
            FolhaFormat format,
            out Extents2d limits,
            out bool standardizedBoundary)
        {
            standardizedBoundary = false;
            limits = default(Extents2d);

            ObjectId definitionId = block.BlockTableRecord;
            var definition = (BlockTableRecord)transaction.GetObject(
                definitionId,
                OpenMode.ForRead);
            var transforms = new List<Matrix3d> { block.BlockTransform };

            if (TryGetBoundaryLimits(
                definition,
                transaction,
                transforms,
                0,
                out limits))
            {
                standardizedBoundary = true;
                return true;
            }

            if (TryGetLimitsFromInsertionPoint(block, format, out limits))
            {
                return true;
            }

            return TryGetGeometricExtents(block, out limits);
        }

        private static bool TryGetBoundaryLimits(
            BlockTableRecord definition,
            Transaction transaction,
            IList<Matrix3d> transforms,
            int depth,
            out Extents2d limits)
        {
            limits = default(Extents2d);
            bool found = false;

            if (definition == null || depth > 8)
            {
                return false;
            }

            foreach (ObjectId nestedId in definition)
            {
                var entity = transaction.GetObject(
                    nestedId,
                    OpenMode.ForRead,
                    false) as Entity;
                if (entity == null)
                {
                    continue;
                }

                if (string.Equals(
                    entity.Layer,
                    BoundaryLayerName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    Extents2d candidate;
                    if (TryGetTransformedEntityExtents(
                        entity,
                        transforms,
                        out candidate))
                    {
                        limits = found ? Union(limits, candidate) : candidate;
                        found = true;
                    }
                }

                var nestedBlock = entity as BlockReference;
                if (nestedBlock == null)
                {
                    continue;
                }

                Extents2d nestedLimits;
                if (TryGetNestedBoundaryLimits(
                    nestedBlock,
                    transaction,
                    transforms,
                    depth,
                    out nestedLimits))
                {
                    limits = found ? Union(limits, nestedLimits) : nestedLimits;
                    found = true;
                }
            }

            return found;
        }

        private static bool TryGetNestedBoundaryLimits(
            BlockReference nestedBlock,
            Transaction transaction,
            IEnumerable<Matrix3d> parentTransforms,
            int parentDepth,
            out Extents2d limits)
        {
            limits = default(Extents2d);

            try
            {
                ObjectId definitionId =
                    BlockNameHelper.GetEffectiveDefinitionId(nestedBlock);
                if (definitionId.IsNull)
                {
                    return false;
                }

                var definition = (BlockTableRecord)transaction.GetObject(
                    definitionId,
                    OpenMode.ForRead);
                var transforms = new List<Matrix3d>
                {
                    nestedBlock.BlockTransform
                };
                transforms.AddRange(parentTransforms);

                return TryGetBoundaryLimits(
                    definition,
                    transaction,
                    transforms,
                    parentDepth + 1,
                    out limits);
            }
            catch
            {
                // Blocos proxy ou definições inválidas não impedem os fallbacks.
                return false;
            }
        }

        private static bool TryGetTransformedEntityExtents(
            Entity entity,
            IList<Matrix3d> transforms,
            out Extents2d result)
        {
            result = default(Extents2d);
            if (entity == null)
            {
                return false;
            }

            try
            {
                return TryTransformExtents(
                    entity.GeometricExtents,
                    transforms,
                    out result);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetLimitsFromInsertionPoint(
            BlockReference block,
            FolhaFormat format,
            out Extents2d limits)
        {
            limits = default(Extents2d);
            if (format == null)
            {
                return false;
            }

            try
            {
                Point3d[] corners =
                {
                    new Point3d(0.0, 0.0, 0.0),
                    new Point3d(format.LongSide, 0.0, 0.0),
                    new Point3d(0.0, format.ShortSide, 0.0),
                    new Point3d(format.LongSide, format.ShortSide, 0.0)
                };

                return TryTransformPoints(
                    corners,
                    block.BlockTransform,
                    out limits);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetGeometricExtents(
            BlockReference block,
            out Extents2d limits)
        {
            limits = default(Extents2d);
            try
            {
                Extents3d extents = block.GeometricExtents;
                limits = new Extents2d(
                    new Point2d(extents.MinPoint.X, extents.MinPoint.Y),
                    new Point2d(extents.MaxPoint.X, extents.MaxPoint.Y));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryTransformExtents(
            Extents3d source,
            IList<Matrix3d> transforms,
            out Extents2d result)
        {
            Point3d[] corners =
            {
                new Point3d(
                    source.MinPoint.X,
                    source.MinPoint.Y,
                    source.MinPoint.Z),
                new Point3d(
                    source.MinPoint.X,
                    source.MaxPoint.Y,
                    source.MinPoint.Z),
                new Point3d(
                    source.MaxPoint.X,
                    source.MinPoint.Y,
                    source.MinPoint.Z),
                new Point3d(
                    source.MaxPoint.X,
                    source.MaxPoint.Y,
                    source.MinPoint.Z)
            };

            return TryTransformPoints(corners, transforms, out result);
        }

        private static bool TryTransformPoints(
            IReadOnlyList<Point3d> corners,
            Matrix3d transform,
            out Extents2d result)
        {
            return TryTransformPoints(
                corners,
                point => point.TransformBy(transform),
                out result);
        }

        private static bool TryTransformPoints(
            IReadOnlyList<Point3d> corners,
            IList<Matrix3d> transforms,
            out Extents2d result)
        {
            return TryTransformPoints(
                corners,
                point => TransformByAll(point, transforms),
                out result);
        }

        private static bool TryTransformPoints(
            IReadOnlyList<Point3d> corners,
            Func<Point3d, Point3d> transform,
            out Extents2d result)
        {
            result = default(Extents2d);
            if (corners == null || corners.Count == 0)
            {
                return false;
            }

            try
            {
                Point3d first = transform(corners[0]);
                double minimumX = first.X;
                double minimumY = first.Y;
                double maximumX = first.X;
                double maximumY = first.Y;

                for (int index = 1; index < corners.Count; index++)
                {
                    Point3d point = transform(corners[index]);
                    minimumX = Math.Min(minimumX, point.X);
                    minimumY = Math.Min(minimumY, point.Y);
                    maximumX = Math.Max(maximumX, point.X);
                    maximumY = Math.Max(maximumY, point.Y);
                }

                result = new Extents2d(
                    new Point2d(minimumX, minimumY),
                    new Point2d(maximumX, maximumY));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Point3d TransformByAll(
            Point3d point,
            IEnumerable<Matrix3d> transforms)
        {
            Point3d result = point;
            if (transforms == null)
            {
                return result;
            }

            foreach (Matrix3d transform in transforms)
            {
                result = result.TransformBy(transform);
            }

            return result;
        }

        private static Extents2d Union(Extents2d first, Extents2d second)
        {
            return new Extents2d(
                new Point2d(
                    Math.Min(first.MinPoint.X, second.MinPoint.X),
                    Math.Min(first.MinPoint.Y, second.MinPoint.Y)),
                new Point2d(
                    Math.Max(first.MaxPoint.X, second.MaxPoint.X),
                    Math.Max(first.MaxPoint.Y, second.MaxPoint.Y)));
        }
    }
}
