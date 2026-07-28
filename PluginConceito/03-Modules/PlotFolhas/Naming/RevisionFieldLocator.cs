using System;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class RevisionFieldLocator
    {
        public const string IdentificationErrorMessage =
            "Não foi possível identificar o campo de revisão no formato.";

        public RevisionFieldLocation Locate(
            string baseName,
            string separator,
            RevisionNameTarget target)
        {
            string safeBaseName = baseName ?? string.Empty;
            RevisionNameTarget safeTarget =
                target ?? RevisionNameTarget.Unspecified;

            return string.IsNullOrEmpty(separator)
                ? LocateWithoutSeparator(safeBaseName, safeTarget.FieldValue)
                : LocateSegment(
                    safeBaseName,
                    separator,
                    safeTarget.SegmentIndex);
        }

        private static RevisionFieldLocation LocateSegment(
            string baseName,
            string separator,
            int segmentIndex)
        {
            if (segmentIndex < 0)
            {
                return RevisionFieldLocation.Failure(
                    "marque um campo como revisão usando a opção R",
                    RevisionNameFailureKind.InvalidConfiguration);
            }

            string[] segments = baseName.Split(
                new[] { separator },
                StringSplitOptions.None);
            if (segmentIndex >= segments.Length)
            {
                return RevisionFieldLocation.Failure(
                    "o nome não possui o campo de revisão selecionado",
                    RevisionNameFailureKind.InvalidConfiguration);
            }

            int startIndex = 0;
            for (int index = 0; index < segmentIndex; index++)
            {
                startIndex += segments[index].Length + separator.Length;
            }

            return RevisionFieldLocation.Success(
                startIndex,
                segments[segmentIndex].Length);
        }

        private static RevisionFieldLocation LocateWithoutSeparator(
            string baseName,
            string revisionFieldValue)
        {
            if (string.IsNullOrWhiteSpace(revisionFieldValue))
            {
                return IdentificationFailure();
            }

            int occurrenceIndex = -1;
            int occurrenceCount = 0;
            int searchIndex = 0;
            while (searchIndex <= baseName.Length - revisionFieldValue.Length)
            {
                int foundIndex = baseName.IndexOf(
                    revisionFieldValue,
                    searchIndex,
                    StringComparison.Ordinal);
                if (foundIndex < 0)
                {
                    break;
                }

                occurrenceIndex = foundIndex;
                occurrenceCount++;
                if (occurrenceCount > 1)
                {
                    return IdentificationFailure();
                }

                searchIndex = foundIndex + 1;
            }

            return occurrenceCount == 1
                ? RevisionFieldLocation.Success(
                    occurrenceIndex,
                    revisionFieldValue.Length)
                : IdentificationFailure();
        }

        private static RevisionFieldLocation IdentificationFailure()
        {
            return RevisionFieldLocation.Failure(
                IdentificationErrorMessage,
                RevisionNameFailureKind.Identification);
        }
    }

    internal sealed class RevisionFieldLocation
    {
        private RevisionFieldLocation(
            int startIndex,
            int length,
            string error,
            RevisionNameFailureKind failureKind)
        {
            StartIndex = startIndex;
            Length = length;
            Error = error;
            FailureKind = failureKind;
        }

        public int StartIndex { get; }

        public int Length { get; }

        public string Error { get; }

        public RevisionNameFailureKind FailureKind { get; }

        public bool IsSuccess
        {
            get { return FailureKind == RevisionNameFailureKind.None; }
        }

        public static RevisionFieldLocation Success(
            int startIndex,
            int length)
        {
            return new RevisionFieldLocation(
                startIndex,
                length,
                null,
                RevisionNameFailureKind.None);
        }

        public static RevisionFieldLocation Failure(
            string error,
            RevisionNameFailureKind failureKind)
        {
            return new RevisionFieldLocation(
                -1,
                0,
                error,
                failureKind);
        }
    }
}
