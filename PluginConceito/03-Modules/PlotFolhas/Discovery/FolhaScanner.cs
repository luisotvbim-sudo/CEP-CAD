using System;
using System.Collections.Generic;
using System.Linq;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class FolhaScanner
    {
        internal const string BoundaryLayerName = "502-CEP-FOR-06";

        private readonly IZwcadContext _zwcad;
        private readonly FolhaFormatCatalog _formats;

        public FolhaScanner(IZwcadContext zwcad, FolhaFormatCatalog formats)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
            _formats = formats ?? throw new ArgumentNullException(nameof(formats));
        }

        public IReadOnlyList<FolhaInfo> ScanActiveLayout()
        {
            Document document = _zwcad.ActiveDocument;
            if (document == null)
            {
                throw new InvalidOperationException("Não existe desenho ativo.");
            }

            string layoutName = LayoutManager.Current.CurrentLayout;
            if (string.Equals(layoutName, "Model", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Ative uma aba de layout antes de executar o comando.");
            }

            var found = new List<FolhaInfo>();
            Database database = document.Database;

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                ObjectId layoutId = LayoutManager.Current.GetLayoutId(layoutName);
                var layout = (Layout)transaction.GetObject(layoutId, OpenMode.ForRead);
                var paperSpace = (BlockTableRecord)transaction.GetObject(
                    layout.BlockTableRecordId,
                    OpenMode.ForRead);

                foreach (ObjectId entityId in paperSpace)
                {
                    var block = transaction.GetObject(entityId, OpenMode.ForRead, false) as BlockReference;
                    if (block == null)
                    {
                        continue;
                    }

                    string blockName = GetEffectiveBlockName(block, transaction);
                    FolhaFormat format;
                    if (!_formats.TryParse(blockName, out format))
                    {
                        continue;
                    }

                    bool standardizedBoundary;
                    Extents2d limits;
                    if (!TryGetSheetLimits(block, transaction, format, out limits, out standardizedBoundary))
                    {
                        continue;
                    }

                    var sheet = new FolhaInfo
                    {
                        BlockReferenceId = entityId,
                        LayoutId = layoutId,
                        LayoutName = layout.LayoutName,
                        BlockName = blockName,
                        Formato = format.Name,
                        Limites = limits,
                        LimitePadronizadoEncontrado = standardizedBoundary
                    };

                    ValidateBlock(block, sheet, format);
                    found.Add(sheet);
                }

                transaction.Commit();
            }

            List<FolhaInfo> ordered = OrderBySheetPosition(found);

            for (int index = 0; index < ordered.Count; index++)
            {
                ordered[index].Sequencia = index + 1;
                _zwcad.WriteMessage(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Folha {0:00}: {1} ({2}) LL={3:0.###},{4:0.###} UR={5:0.###},{6:0.###} limite={7}",
                    ordered[index].Sequencia,
                    ordered[index].BlockName,
                    ordered[index].Formato,
                    ordered[index].Limites.MinPoint.X,
                    ordered[index].Limites.MinPoint.Y,
                    ordered[index].Limites.MaxPoint.X,
                    ordered[index].Limites.MaxPoint.Y,
                    ordered[index].LimitePadronizadoEncontrado ? BoundaryLayerName : "estimado"));
            }

            ValidateOverlaps(ordered);
            return ordered;
        }

        private static List<FolhaInfo> OrderBySheetPosition(IEnumerable<FolhaInfo> sheets)
        {
            const double rowTolerance = 10.0;

            var remaining = sheets
                .OrderByDescending(sheet => sheet.Limites.MinPoint.Y)
                .ThenBy(sheet => sheet.Limites.MinPoint.X)
                .ToList();

            var ordered = new List<FolhaInfo>();
            while (remaining.Count > 0)
            {
                double rowY = remaining[0].Limites.MinPoint.Y;
                List<FolhaInfo> row = remaining
                    .Where(sheet => Math.Abs(sheet.Limites.MinPoint.Y - rowY) <= rowTolerance)
                    .OrderBy(sheet => sheet.Limites.MinPoint.X)
                    .ToList();

                ordered.AddRange(row);

                foreach (FolhaInfo sheet in row)
                {
                    remaining.Remove(sheet);
                }
            }

            return ordered;
        }

        private static string GetEffectiveBlockName(BlockReference block, Transaction transaction)
        {
            ObjectId definitionId = block.IsDynamicBlock
                ? block.DynamicBlockTableRecord
                : block.BlockTableRecord;
            var definition = (BlockTableRecord)transaction.GetObject(definitionId, OpenMode.ForRead);
            return definition.Name;
        }

        private static bool TryGetSheetLimits(
            BlockReference block,
            Transaction transaction,
            FolhaFormat format,
            out Extents2d limits,
            out bool standardizedBoundary)
        {
            standardizedBoundary = false;
            limits = default(Extents2d);

            ObjectId definitionId = block.BlockTableRecord;
            var definition = (BlockTableRecord)transaction.GetObject(definitionId, OpenMode.ForRead);
            var transforms = new List<Matrix3d> { block.BlockTransform };

            if (TryGetBoundaryLimits(definition, transaction, transforms, 0, out limits))
            {
                standardizedBoundary = true;
                return true;
            }

            if (TryGetLimitsFromInsertionPoint(block, format, out limits))
            {
                return true;
            }

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
                var entity = transaction.GetObject(nestedId, OpenMode.ForRead, false) as Entity;
                if (entity == null)
                {
                    continue;
                }

                if (string.Equals(entity.Layer, BoundaryLayerName, StringComparison.OrdinalIgnoreCase))
                {
                    Extents2d candidate;
                    if (TryGetTransformedEntityExtents(entity, transforms, out candidate))
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

                try
                {
                    ObjectId nestedDefinitionId = nestedBlock.IsDynamicBlock
                        ? nestedBlock.DynamicBlockTableRecord
                        : nestedBlock.BlockTableRecord;

                    if (nestedDefinitionId.IsNull)
                    {
                        continue;
                    }

                    var nestedDefinition = (BlockTableRecord)transaction.GetObject(
                        nestedDefinitionId,
                        OpenMode.ForRead);

                    var nestedTransforms = new List<Matrix3d>();
                    nestedTransforms.Add(nestedBlock.BlockTransform);
                    nestedTransforms.AddRange(transforms);

                    Extents2d nestedLimits;
                    if (TryGetBoundaryLimits(
                        nestedDefinition,
                        transaction,
                        nestedTransforms,
                        depth + 1,
                        out nestedLimits))
                    {
                        limits = found ? Union(limits, nestedLimits) : nestedLimits;
                        found = true;
                    }
                }
                catch
                {
                    continue;
                }
            }

            return found;
        }

        private static bool TryGetTransformedEntityExtents(
            Entity entity,
            Matrix3d transform,
            out Extents2d result)
        {
            result = default(Extents2d);

            if (entity == null)
            {
                return false;
            }

            try
            {
                return TryTransformExtents(entity.GeometricExtents, transform, out result);
            }
            catch
            {
                result = default(Extents2d);
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
                return TryTransformExtents(entity.GeometricExtents, transforms, out result);
            }
            catch
            {
                result = default(Extents2d);
                return false;
            }
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

                return TryTransformPoints(corners, block.BlockTransform, out limits);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryTransformExtents(
            Extents3d source,
            Matrix3d transform,
            out Extents2d result)
        {
            try
            {
                Point3d[] corners =
                {
                    new Point3d(source.MinPoint.X, source.MinPoint.Y, source.MinPoint.Z),
                    new Point3d(source.MinPoint.X, source.MaxPoint.Y, source.MinPoint.Z),
                    new Point3d(source.MaxPoint.X, source.MinPoint.Y, source.MinPoint.Z),
                    new Point3d(source.MaxPoint.X, source.MaxPoint.Y, source.MinPoint.Z)
                };

                Point3d first = corners[0].TransformBy(transform);
                double minX = first.X;
                double minY = first.Y;
                double maxX = first.X;
                double maxY = first.Y;

                for (int index = 1; index < corners.Length; index++)
                {
                    Point3d transformed = corners[index].TransformBy(transform);
                    minX = Math.Min(minX, transformed.X);
                    minY = Math.Min(minY, transformed.Y);
                    maxX = Math.Max(maxX, transformed.X);
                    maxY = Math.Max(maxY, transformed.Y);
                }

                result = new Extents2d(new Point2d(minX, minY), new Point2d(maxX, maxY));
                return true;
            }
            catch
            {
                result = default(Extents2d);
                return false;
            }
        }

        private static bool TryTransformExtents(
            Extents3d source,
            IList<Matrix3d> transforms,
            out Extents2d result)
        {
            try
            {
                Point3d[] corners =
                {
                    new Point3d(source.MinPoint.X, source.MinPoint.Y, source.MinPoint.Z),
                    new Point3d(source.MinPoint.X, source.MaxPoint.Y, source.MinPoint.Z),
                    new Point3d(source.MaxPoint.X, source.MinPoint.Y, source.MinPoint.Z),
                    new Point3d(source.MaxPoint.X, source.MaxPoint.Y, source.MinPoint.Z)
                };

                return TryTransformPoints(corners, transforms, out result);
            }
            catch
            {
                result = default(Extents2d);
                return false;
            }
        }

        private static bool TryTransformPoints(
            Point3d[] corners,
            Matrix3d transform,
            out Extents2d result)
        {
            if (corners == null || corners.Length == 0)
            {
                result = default(Extents2d);
                return false;
            }

            try
            {
                Point3d first = corners[0].TransformBy(transform);
                double minX = first.X;
                double minY = first.Y;
                double maxX = first.X;
                double maxY = first.Y;

                for (int index = 1; index < corners.Length; index++)
                {
                    Point3d transformed = corners[index].TransformBy(transform);
                    minX = Math.Min(minX, transformed.X);
                    minY = Math.Min(minY, transformed.Y);
                    maxX = Math.Max(maxX, transformed.X);
                    maxY = Math.Max(maxY, transformed.Y);
                }

                result = new Extents2d(new Point2d(minX, minY), new Point2d(maxX, maxY));
                return true;
            }
            catch
            {
                result = default(Extents2d);
                return false;
            }
        }

        private static bool TryTransformPoints(
            Point3d[] corners,
            IList<Matrix3d> transforms,
            out Extents2d result)
        {
            if (corners == null || corners.Length == 0)
            {
                result = default(Extents2d);
                return false;
            }

            try
            {
                Point3d first = TransformByAll(corners[0], transforms);
                double minX = first.X;
                double minY = first.Y;
                double maxX = first.X;
                double maxY = first.Y;

                for (int index = 1; index < corners.Length; index++)
                {
                    Point3d transformed = TransformByAll(corners[index], transforms);
                    minX = Math.Min(minX, transformed.X);
                    minY = Math.Min(minY, transformed.Y);
                    maxX = Math.Max(maxX, transformed.X);
                    maxY = Math.Max(maxY, transformed.Y);
                }

                result = new Extents2d(new Point2d(minX, minY), new Point2d(maxX, maxY));
                return true;
            }
            catch
            {
                result = default(Extents2d);
                return false;
            }
        }

        private static Point3d TransformByAll(Point3d point, IList<Matrix3d> transforms)
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

        private void ValidateBlock(BlockReference block, FolhaInfo sheet, FolhaFormat format)
        {
            string dimensionError;
            if (!_formats.DimensionsMatch(format, sheet.Largura, sheet.Altura, out dimensionError))
            {
                sheet.Erros.Add(dimensionError);
            }

            const double scaleTolerance = 0.0001;
            Scale3d scale = block.ScaleFactors;
            if (Math.Abs(Math.Abs(scale.X) - 1.0) > scaleTolerance ||
                Math.Abs(Math.Abs(scale.Y) - 1.0) > scaleTolerance)
            {
                sheet.Erros.Add("bloco deve estar na escala 1:1");
            }

            double quarterTurn = Math.PI / 2.0;
            double normalizedRotation = Math.Abs(block.Rotation % quarterTurn);
            if (normalizedRotation > 0.0001 && Math.Abs(normalizedRotation - quarterTurn) > 0.0001)
            {
                sheet.Erros.Add("rotação deve ser múltipla de 90 graus");
            }

            if (!sheet.LimitePadronizadoEncontrado)
            {
                sheet.Avisos.Add(
                    "layer " + BoundaryLayerName + " não encontrada; foram usados limites estimados do bloco");
            }
        }

        private static void ValidateOverlaps(IReadOnlyList<FolhaInfo> sheets)
        {
            const double tolerance = 1.0;

            for (int firstIndex = 0; firstIndex < sheets.Count; firstIndex++)
            {
                for (int secondIndex = firstIndex + 1; secondIndex < sheets.Count; secondIndex++)
                {
                    FolhaInfo first = sheets[firstIndex];
                    FolhaInfo second = sheets[secondIndex];
                    double overlapWidth = Math.Min(first.Limites.MaxPoint.X, second.Limites.MaxPoint.X) -
                        Math.Max(first.Limites.MinPoint.X, second.Limites.MinPoint.X);
                    double overlapHeight = Math.Min(first.Limites.MaxPoint.Y, second.Limites.MaxPoint.Y) -
                        Math.Max(first.Limites.MinPoint.Y, second.Limites.MinPoint.Y);

                    if (overlapWidth > tolerance && overlapHeight > tolerance)
                    {
                        first.Erros.Add("sobrepõe a folha " + second.Sequencia);
                        second.Erros.Add("sobrepõe a folha " + first.Sequencia);
                    }
                }
            }

            foreach (FolhaInfo sheet in sheets)
            {
                sheet.NotifyValidationChanged();
            }
        }
    }
}
