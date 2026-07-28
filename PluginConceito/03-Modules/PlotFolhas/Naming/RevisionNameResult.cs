namespace PluginConceito.Modules.PlotFolhas
{
    internal enum RevisionNameFailureKind
    {
        None,
        Identification,
        InvalidConfiguration,
        InvalidValue
    }

    internal sealed class RevisionNameResult
    {
        private RevisionNameResult(
            string fileName,
            string error,
            RevisionNameFailureKind failureKind)
        {
            FileName = fileName;
            Error = error;
            FailureKind = failureKind;
        }

        public string FileName { get; }

        public string Error { get; }

        public RevisionNameFailureKind FailureKind { get; }

        public bool IsSuccess
        {
            get { return FailureKind == RevisionNameFailureKind.None; }
        }

        public bool RequiresIdentificationWarning
        {
            get { return FailureKind == RevisionNameFailureKind.Identification; }
        }

        public static RevisionNameResult Success(string fileName)
        {
            return new RevisionNameResult(
                fileName,
                null,
                RevisionNameFailureKind.None);
        }

        public static RevisionNameResult Failure(
            string originalFileName,
            string error,
            RevisionNameFailureKind failureKind)
        {
            return new RevisionNameResult(
                originalFileName,
                error,
                failureKind);
        }
    }
}
