using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class FolhaValidationService
    {
        private const double ScaleTolerance = 0.0001;
        private const double RotationTolerance = 0.0001;
        private const double OverlapTolerance = 1.0;

        private readonly FolhaFormatCatalog _formats;

        public FolhaValidationService(FolhaFormatCatalog formats)
        {
            _formats = formats ?? throw new ArgumentNullException(nameof(formats));
        }

        public void ValidateSheet(
            BlockReference block,
            FolhaInfo sheet,
            FolhaFormat format)
        {
            string dimensionError;
            if (!_formats.DimensionsMatch(
                format,
                sheet.LarguraPapel,
                sheet.AlturaPapel,
                out dimensionError))
            {
                sheet.Erros.Add(dimensionError);
            }

            ValidateScale(block, sheet);
            ValidateRotation(block, sheet);

            if (!sheet.LimitePadronizadoEncontrado)
            {
                sheet.Avisos.Add(
                    "layer " +
                    FolhaBoundaryResolver.BoundaryLayerName +
                    " não encontrada; foram usados limites estimados do bloco");
            }
        }

        public void ValidateOverlaps(IReadOnlyList<FolhaInfo> sheets)
        {
            for (int firstIndex = 0;
                firstIndex < sheets.Count;
                firstIndex++)
            {
                for (int secondIndex = firstIndex + 1;
                    secondIndex < sheets.Count;
                    secondIndex++)
                {
                    AddOverlapErrors(
                        sheets[firstIndex],
                        sheets[secondIndex]);
                }
            }

            foreach (FolhaInfo sheet in sheets)
            {
                sheet.NotifyValidationChanged();
            }
        }

        private static void ValidateScale(
            BlockReference block,
            FolhaInfo sheet)
        {
            Scale3d scale = block.ScaleFactors;
            double scaleX = Math.Abs(scale.X);
            double scaleY = Math.Abs(scale.Y);

            if (sheet.IsModelSpace)
            {
                double reference = Math.Max(scaleX, scaleY);
                if (scaleX <= ScaleTolerance ||
                    scaleY <= ScaleTolerance ||
                    Math.Abs(scaleX - scaleY) >
                        Math.Max(ScaleTolerance, reference * ScaleTolerance))
                {
                    sheet.Erros.Add(
                        "bloco no Model deve ter escala X/Y uniforme");
                }
                return;
            }

            if (Math.Abs(scaleX - 1.0) > ScaleTolerance ||
                Math.Abs(scaleY - 1.0) > ScaleTolerance)
            {
                sheet.Erros.Add("bloco no Layout deve estar na escala 1:1");
            }
        }

        private static void ValidateRotation(
            BlockReference block,
            FolhaInfo sheet)
        {
            double quarterTurn = Math.PI / 2.0;
            double rotation = Math.Abs(block.Rotation % quarterTurn);
            if (rotation > RotationTolerance &&
                Math.Abs(rotation - quarterTurn) > RotationTolerance)
            {
                sheet.Erros.Add(
                    "rotação deve ser múltipla de 90 graus");
            }
        }

        private static void AddOverlapErrors(
            FolhaInfo first,
            FolhaInfo second)
        {
            double overlapWidth =
                Math.Min(
                    first.Limites.MaxPoint.X,
                    second.Limites.MaxPoint.X) -
                Math.Max(
                    first.Limites.MinPoint.X,
                    second.Limites.MinPoint.X);
            double overlapHeight =
                Math.Min(
                    first.Limites.MaxPoint.Y,
                    second.Limites.MaxPoint.Y) -
                Math.Max(
                    first.Limites.MinPoint.Y,
                    second.Limites.MinPoint.Y);

            if (overlapWidth <= OverlapTolerance ||
                overlapHeight <= OverlapTolerance)
            {
                return;
            }

            first.Erros.Add("sobrepõe a folha " + second.Sequencia);
            second.Erros.Add("sobrepõe a folha " + first.Sequencia);
        }
    }
}
