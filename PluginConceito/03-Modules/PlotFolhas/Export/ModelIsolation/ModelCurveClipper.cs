using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ModelCurveClipper
    {
        private readonly CurveSplitPointCollector _splitPointCollector;
        private readonly CurveAtomicSplitter _atomicSplitter;
        private readonly CurvePieceClassifier _pieceClassifier;

        public ModelCurveClipper()
        {
            _splitPointCollector = new CurveSplitPointCollector();
            _atomicSplitter = new CurveAtomicSplitter(_splitPointCollector);
            _pieceClassifier = new CurvePieceClassifier();
        }

        public void Clip(
            Curve curve,
            BlockTableRecord modelSpace,
            Transaction transaction,
            IReadOnlyList<ViewportModelRegion> regions,
            ModelIsolationResult result,
            Action<string> report)
        {
            Point3dCollection splitPoints = _splitPointCollector.Collect(curve, regions);
            if (splitPoints.Count == 0)
            {
                KeepUnsplitCurve(curve, regions, result, report);
                return;
            }

            List<Curve> pieces;
            string splitFailure;
            if (!_atomicSplitter.TrySplit(
                curve,
                splitPoints,
                regions,
                out pieces,
                out splitFailure))
            {
                result.EntitiesKept++;
                result.CurvesNotSplit++;
                report(FormatDiagnostic(
                    "CURVE_SPLIT_FAILED",
                    curve,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "unionTransitions={0} failure={1}",
                        splitPoints.Count,
                        Sanitize(splitFailure))));
                return;
            }

            List<PieceDecision> decisions;
            string classificationDetails;
            if (!TryClassifyPieces(
                pieces,
                regions,
                out decisions,
                out classificationDetails))
            {
                DisposeAll(pieces);
                result.EntitiesKept++;
                result.CurvesNotSplit++;
                report(FormatDiagnostic(
                    "CURVE_PIECE_CLASSIFICATION_FAILED",
                    curve,
                    "unionTransitions=" + splitPoints.Count + " " +
                        classificationDetails));
                return;
            }

            string diagnosticPrefix = FormatDiagnostic("CURVE_SPLIT", curve, string.Empty);
            CadEntityAccess.Erase(curve);
            result.EntitiesErased++;
            result.CurvesSplit++;

            int visiblePieces = 0;
            foreach (PieceDecision decision in decisions)
            {
                if (decision.IsVisible)
                {
                    modelSpace.AppendEntity(decision.Piece);
                    transaction.AddNewlyCreatedDBObject(decision.Piece, true);
                    result.CurvePiecesCreated++;
                    visiblePieces++;
                }
                else
                {
                    decision.Piece.Dispose();
                }
            }

            report(diagnosticPrefix + string.Format(
                CultureInfo.InvariantCulture,
                "unionTransitions={0} atomicPieces={1} visiblePieces={2} pieceSamples={3}",
                splitPoints.Count,
                pieces.Count,
                visiblePieces,
                classificationDetails));
        }

        private static void KeepUnsplitCurve(
            Curve curve,
            IReadOnlyList<ViewportModelRegion> regions,
            ModelIsolationResult result,
            Action<string> report)
        {
            result.EntitiesKept++;
            string visibilitySamples = GetVisibilitySamples(curve, regions);
            if (visibilitySamples.IndexOf('1') >= 0 &&
                visibilitySamples.IndexOf('0') >= 0)
            {
                report(FormatDiagnostic(
                    "CURVE_KEPT_WITHOUT_UNION_TRANSITIONS",
                    curve,
                    "samples=" + visibilitySamples));
            }
        }

        private bool TryClassifyPieces(
            IReadOnlyList<Curve> pieces,
            IReadOnlyList<ViewportModelRegion> regions,
            out List<PieceDecision> decisions,
            out string details)
        {
            decisions = new List<PieceDecision>(pieces.Count);
            var diagnostic = new StringBuilder();

            for (int index = 0; index < pieces.Count; index++)
            {
                bool isVisible;
                string pieceDiagnostic;
                if (!_pieceClassifier.TryClassify(
                    pieces[index],
                    regions,
                    out isVisible,
                    out pieceDiagnostic))
                {
                    details = "piece=" + index + ":" + pieceDiagnostic;
                    return false;
                }

                decisions.Add(new PieceDecision(pieces[index], isVisible));
                if (diagnostic.Length > 0) diagnostic.Append(';');
                diagnostic.Append(index);
                diagnostic.Append(':');
                diagnostic.Append(pieceDiagnostic);
            }

            details = diagnostic.ToString();
            return true;
        }

        private static string GetVisibilitySamples(
            Curve curve,
            IEnumerable<ViewportModelRegion> regions)
        {
            var result = new StringBuilder(17);
            try
            {
                double start = curve.StartParam;
                double end = curve.EndParam;
                for (int index = 0; index <= 16; index++)
                {
                    Point3d point = curve.GetPointAtParameter(
                        start + ((end - start) * index / 16.0));
                    result.Append(IsInsideAnyRegion(point, regions) ? '1' : '0');
                }
            }
            catch
            {
                return "unavailable";
            }

            return result.ToString();
        }

        private static bool IsInsideAnyRegion(
            Point3d point,
            IEnumerable<ViewportModelRegion> regions)
        {
            foreach (ViewportModelRegion region in regions)
            {
                if (region.Contains(point)) return true;
            }

            return false;
        }

        private static void DisposeAll(IEnumerable<Curve> curves)
        {
            foreach (Curve curve in curves) curve.Dispose();
        }

        private static string FormatDiagnostic(
            string eventName,
            Curve curve,
            string details)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "[MODEL-ISOLATION-DEBUG] {0} type={1} handle={2} layer={3} extents={4} {5}",
                eventName,
                curve.GetType().Name,
                curve.Handle,
                curve.Layer,
                GetExtentsDescription(curve),
                details);
        }

        private static string GetExtentsDescription(Entity entity)
        {
            try
            {
                Extents3d extents = entity.GeometricExtents;
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "({0:R},{1:R})-({2:R},{3:R})",
                    extents.MinPoint.X,
                    extents.MinPoint.Y,
                    extents.MaxPoint.X,
                    extents.MaxPoint.Y);
            }
            catch
            {
                return "unavailable";
            }
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrEmpty(value)
                ? "none"
                : value.Replace('\r', ' ').Replace('\n', ' ');
        }

        private sealed class PieceDecision
        {
            public PieceDecision(Curve piece, bool isVisible)
            {
                Piece = piece;
                IsVisible = isVisible;
            }

            public Curve Piece { get; private set; }
            public bool IsVisible { get; private set; }
        }
    }
}
