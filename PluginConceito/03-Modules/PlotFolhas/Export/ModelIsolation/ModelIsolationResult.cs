namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ModelIsolationResult
    {
        public string LayoutName { get; set; }
        public int ViewportsConsidered { get; set; }
        public int EntitiesKept { get; set; }
        public int EntitiesErased { get; set; }
        public int CurvesSplit { get; set; }
        public int CurvePiecesCreated { get; set; }
        public int CurvesNotSplit { get; set; }
        public int BlockReferencesKept { get; set; }
        public int EntitiesErasedByVisibility { get; set; }
    }
}
