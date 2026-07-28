namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class SheetRevisionResult
    {
        public SheetRevisionResult(
            string message,
            string warningMessage)
        {
            Message = message ?? string.Empty;
            WarningMessage = warningMessage;
        }

        public string Message { get; }

        public string WarningMessage { get; }

        public bool HasWarning
        {
            get { return !string.IsNullOrWhiteSpace(WarningMessage); }
        }
    }
}
