using System.Text.RegularExpressions;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class RevisionNumberIncrementer
    {
        private static readonly Regex NumericGroupPattern =
            new Regex(@"\d+", RegexOptions.Compiled);

        public RevisionValueIncrementResult Increment(string revisionValue)
        {
            string safeValue = revisionValue ?? string.Empty;
            MatchCollection numericGroups =
                NumericGroupPattern.Matches(safeValue);
            if (numericGroups.Count == 0)
            {
                return RevisionValueIncrementResult.Failure(
                    "o campo de revisão não possui número para incrementar");
            }

            Match numberMatch = numericGroups[numericGroups.Count - 1];
            if (!long.TryParse(numberMatch.Value, out long currentNumber) ||
                currentNumber == long.MaxValue)
            {
                return RevisionValueIncrementResult.Failure(
                    "o número da revisão é inválido ou muito grande");
            }

            string nextNumber = (currentNumber + 1).ToString(
                "D" + numberMatch.Value.Length);
            return RevisionValueIncrementResult.Success(
                safeValue.Substring(0, numberMatch.Index) +
                nextNumber +
                safeValue.Substring(numberMatch.Index + numberMatch.Length));
        }
    }

    internal sealed class RevisionValueIncrementResult
    {
        private RevisionValueIncrementResult(
            string value,
            string error)
        {
            Value = value;
            Error = error;
        }

        public string Value { get; }

        public string Error { get; }

        public bool IsSuccess
        {
            get { return string.IsNullOrWhiteSpace(Error); }
        }

        public static RevisionValueIncrementResult Success(string value)
        {
            return new RevisionValueIncrementResult(value, null);
        }

        public static RevisionValueIncrementResult Failure(string error)
        {
            return new RevisionValueIncrementResult(null, error);
        }
    }
}
