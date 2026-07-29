using System.Collections.Generic;
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
            ModelIsolationResult result)
        {
            Point3dCollection splitPoints =
                _splitPointCollector.Collect(curve, regions);
            if (splitPoints.Count == 0)
            {
                result.EntitiesKept++;
                return;
            }

            List<Curve> pieces;
            if (!_atomicSplitter.TrySplit(
                curve,
                splitPoints,
                regions,
                out pieces,
                out _))
            {
                result.EntitiesKept++;
                result.CurvesNotSplit++;
                return;
            }

            List<PieceDecision> decisions;
            if (!TryClassifyPieces(pieces, regions, out decisions))
            {
                DisposeAll(pieces);
                result.EntitiesKept++;
                result.CurvesNotSplit++;
                return;
            }

            CadEntityAccess.Erase(curve);
            result.EntitiesErased++;
            result.CurvesSplit++;

            foreach (PieceDecision decision in decisions)
            {
                if (decision.IsVisible)
                {
                    modelSpace.AppendEntity(decision.Piece);
                    transaction.AddNewlyCreatedDBObject(
                        decision.Piece,
                        true);
                    result.CurvePiecesCreated++;
                }
                else
                {
                    decision.Piece.Dispose();
                }
            }
        }

        private bool TryClassifyPieces(
            IReadOnlyList<Curve> pieces,
            IReadOnlyList<ViewportModelRegion> regions,
            out List<PieceDecision> decisions)
        {
            decisions = new List<PieceDecision>(pieces.Count);
            for (int index = 0; index < pieces.Count; index++)
            {
                bool isVisible;
                if (!_pieceClassifier.TryClassify(
                    pieces[index],
                    regions,
                    out isVisible,
                    out _))
                {
                    return false;
                }

                decisions.Add(
                    new PieceDecision(pieces[index], isVisible));
            }

            return true;
        }

        private static void DisposeAll(IEnumerable<Curve> curves)
        {
            foreach (Curve curve in curves) curve.Dispose();
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
