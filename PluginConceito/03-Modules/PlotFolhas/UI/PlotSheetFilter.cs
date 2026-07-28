using System;
using System.ComponentModel;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotSheetFilter
    {
        public string SearchText { get; set; }

        public bool ShowOnlyIssues { get; set; }

        public bool ShowOnlyRevisionUpdates { get; set; }

        public bool ShowOnlySelectedOutputs { get; set; }

        public bool Matches(object value)
        {
            var sheet = value as FolhaInfo;
            if (sheet == null)
            {
                return false;
            }

            if (ShowOnlyIssues &&
                sheet.Valida &&
                sheet.Avisos.Count == 0)
            {
                return false;
            }

            if (ShowOnlyRevisionUpdates && !sheet.SubirRevisao)
            {
                return false;
            }

            if (ShowOnlySelectedOutputs &&
                !sheet.Plotar &&
                !sheet.GerarDwg)
            {
                return false;
            }

            return MatchesSearch(sheet);
        }

        public bool IsAffectedBy(PropertyChangedEventArgs change)
        {
            if (change == null)
            {
                return false;
            }

            return (ShowOnlyRevisionUpdates &&
                    change.PropertyName == nameof(FolhaInfo.SubirRevisao)) ||
                (ShowOnlySelectedOutputs &&
                    IsOutputSelection(change.PropertyName));
        }

        private bool MatchesSearch(FolhaInfo sheet)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            string term = SearchText.Trim();
            return Contains(sheet.NomeArquivo, term) ||
                Contains(sheet.Formato, term) ||
                Contains(sheet.Status, term) ||
                sheet.Sequencia.ToString().IndexOf(
                    term,
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsOutputSelection(string propertyName)
        {
            return propertyName == nameof(FolhaInfo.Plotar) ||
                propertyName == nameof(FolhaInfo.GerarDwg);
        }

        private static bool Contains(string value, string term)
        {
            return (value ?? string.Empty).IndexOf(
                term,
                StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
