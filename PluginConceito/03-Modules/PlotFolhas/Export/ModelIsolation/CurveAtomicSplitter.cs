using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class CurveAtomicSplitter
    {
        private const int MaximumRefinementDepth = 16;
        private readonly CurveSplitPointCollector _splitPointCollector;

        public CurveAtomicSplitter(CurveSplitPointCollector splitPointCollector)
        {
            _splitPointCollector = splitPointCollector ??
                throw new ArgumentNullException(nameof(splitPointCollector));
        }

        public bool TrySplit(
            Curve original,
            Point3dCollection initialSplitPoints,
            IReadOnlyList<ViewportModelRegion> regions,
            out List<Curve> atomicPieces,
            out string failure)
        {
            atomicPieces = new List<Curve>();
            var pending = new Queue<WorkItem>();

            List<Curve> firstPieces;
            if (!TrySplitOnce(original, initialSplitPoints, out firstPieces, out failure))
                return false;

            foreach (Curve piece in firstPieces) pending.Enqueue(new WorkItem(piece, 1));

            while (pending.Count > 0)
            {
                WorkItem work = pending.Dequeue();
                Point3dCollection remainingPoints = _splitPointCollector.Collect(
                    work.Curve,
                    regions);

                if (remainingPoints.Count == 0)
                {
                    atomicPieces.Add(work.Curve);
                    continue;
                }

                if (work.Depth >= MaximumRefinementDepth)
                {
                    work.Curve.Dispose();
                    DisposeAll(pending, atomicPieces);
                    failure = "maximum-refinement-depth";
                    return false;
                }

                List<Curve> refinedPieces;
                if (!TrySplitOnce(work.Curve, remainingPoints, out refinedPieces, out failure))
                {
                    work.Curve.Dispose();
                    DisposeAll(pending, atomicPieces);
                    return false;
                }

                work.Curve.Dispose();
                foreach (Curve refinedPiece in refinedPieces)
                    pending.Enqueue(new WorkItem(refinedPiece, work.Depth + 1));
            }

            failure = null;
            return true;
        }

        private static bool TrySplitOnce(
            Curve curve,
            Point3dCollection splitPoints,
            out List<Curve> pieces,
            out string failure)
        {
            pieces = new List<Curve>();
            Point3dCollection pointsForCall = SelectMinimalSplitPoints(
                curve,
                splitPoints,
                out failure);
            if (pointsForCall == null) return false;

            DBObjectCollection rawPieces;
            try
            {
                rawPieces = curve.GetSplitCurves(pointsForCall);
            }
            catch (Exception exception)
            {
                failure = exception.GetType().Name + ": " + exception.Message;
                return false;
            }

            foreach (DBObject item in rawPieces)
            {
                var piece = item as Curve;
                if (piece != null)
                {
                    pieces.Add(piece);
                    continue;
                }

                item.Dispose();
                DisposeAll(pieces);
                pieces.Clear();
                failure = "split-returned-non-curve";
                return false;
            }

            if (pieces.Count < 2)
            {
                DisposeAll(pieces);
                pieces.Clear();
                failure = "split-returned-fewer-than-two-pieces";
                return false;
            }

            failure = null;
            return true;
        }

        private static Point3dCollection SelectMinimalSplitPoints(
            Curve curve,
            Point3dCollection availablePoints,
            out string failure)
        {
            var result = new Point3dCollection();
            bool isClosed;
            try
            {
                isClosed = curve.StartPoint.DistanceTo(curve.EndPoint) <= 1e-7;
            }
            catch
            {
                isClosed = false;
            }

            int requiredPoints = isClosed ? 2 : 1;
            if (availablePoints.Count < requiredPoints)
            {
                failure = isClosed
                    ? "closed-curve-has-fewer-than-two-union-transitions"
                    : "open-curve-has-no-union-transition";
                return null;
            }

            // O ZWCAD e mais previsivel dividindo um ponto por vez. Curvas fechadas
            // precisam de dois pontos para produzir os dois primeiros arcos.
            for (int index = 0; index < requiredPoints; index++)
                result.Add(availablePoints[index]);

            failure = null;
            return result;
        }

        private static void DisposeAll(
            Queue<WorkItem> pending,
            IEnumerable<Curve> completed)
        {
            while (pending.Count > 0) pending.Dequeue().Curve.Dispose();
            DisposeAll(completed);
        }

        private static void DisposeAll(IEnumerable<Curve> curves)
        {
            foreach (Curve curve in curves) curve.Dispose();
        }

        private sealed class WorkItem
        {
            public WorkItem(Curve curve, int depth)
            {
                Curve = curve;
                Depth = depth;
            }

            public Curve Curve { get; private set; }
            public int Depth { get; private set; }
        }
    }
}
