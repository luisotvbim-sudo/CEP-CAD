namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class RevisionNameTarget
    {
        private static readonly RevisionNameTarget Empty =
            new RevisionNameTarget(-1, null);

        public RevisionNameTarget(int segmentIndex, string fieldValue)
        {
            SegmentIndex = segmentIndex;
            FieldValue = fieldValue;
        }

        public static RevisionNameTarget Unspecified
        {
            get { return Empty; }
        }

        public int SegmentIndex { get; }

        public string FieldValue { get; }

    }
}
