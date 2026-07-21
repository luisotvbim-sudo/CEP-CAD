namespace PluginConceito.Modules.PlotFolhas
{
    internal enum ModelIsolationOutcome
    {
        Isolated,
        ModelClearedWithoutViewport,
        ModelPreservedWithoutMatches
    }

    internal sealed class ModelIsolationResult
    {
        public ModelIsolationOutcome Outcome { get; internal set; }

        public int ViewportsConsidered { get; internal set; }

        public int EntitiesKept { get; internal set; }

        public int EntitiesErased { get; internal set; }

        public int EntitiesKeptWithoutExtents { get; internal set; }
    }
}
