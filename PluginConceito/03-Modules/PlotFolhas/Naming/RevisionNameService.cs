using System;
using System.IO;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class RevisionNameService
    {
        private readonly RevisionFieldLocator _fieldLocator;
        private readonly RevisionNumberIncrementer _numberIncrementer;

        public RevisionNameService(
            RevisionFieldLocator fieldLocator,
            RevisionNumberIncrementer numberIncrementer)
        {
            _fieldLocator = fieldLocator ??
                throw new ArgumentNullException(nameof(fieldLocator));
            _numberIncrementer = numberIncrementer ??
                throw new ArgumentNullException(nameof(numberIncrementer));
        }

        public RevisionNameResult Increment(
            string fileName,
            string separator,
            RevisionNameTarget target)
        {
            string safeFileName = fileName ?? string.Empty;
            string baseName = Path.GetFileNameWithoutExtension(safeFileName);
            RevisionFieldLocation location = _fieldLocator.Locate(
                baseName,
                separator,
                target);
            if (!location.IsSuccess)
            {
                return RevisionNameResult.Failure(
                    safeFileName,
                    location.Error,
                    location.FailureKind);
            }

            string revisionValue = baseName.Substring(
                location.StartIndex,
                location.Length);
            RevisionValueIncrementResult increment =
                _numberIncrementer.Increment(revisionValue);
            if (!increment.IsSuccess)
            {
                return RevisionNameResult.Failure(
                    safeFileName,
                    increment.Error,
                    RevisionNameFailureKind.InvalidValue);
            }

            string updatedBaseName =
                baseName.Substring(0, location.StartIndex) +
                increment.Value +
                baseName.Substring(location.StartIndex + location.Length);
            return RevisionNameResult.Success(
                updatedBaseName + ResolveExtension(safeFileName));
        }

        private static string ResolveExtension(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            return string.IsNullOrWhiteSpace(extension)
                ? ".pdf"
                : extension;
        }
    }
}
