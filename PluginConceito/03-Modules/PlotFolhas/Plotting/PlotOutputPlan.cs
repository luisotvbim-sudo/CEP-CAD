using System;
using System.Collections.Generic;
using System.Linq;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotOutputPlan
    {
        private PlotOutputPlan(
            IReadOnlyList<FolhaInfo> pdfSheets,
            IReadOnlyList<FolhaInfo> dwgSheets,
            IReadOnlyList<FolhaInfo> selectedSheets)
        {
            PdfSheets = pdfSheets;
            DwgSheets = dwgSheets;
            SelectedSheets = selectedSheets;
        }

        public IReadOnlyList<FolhaInfo> PdfSheets { get; private set; }

        public IReadOnlyList<FolhaInfo> DwgSheets { get; private set; }

        public IReadOnlyList<FolhaInfo> SelectedSheets { get; private set; }

        public bool HasPdfOutput
        {
            get { return PdfSheets.Count > 0; }
        }

        public static PlotOutputPlan Create(IEnumerable<FolhaInfo> sheets)
        {
            List<FolhaInfo> allSheets = (sheets ?? Enumerable.Empty<FolhaInfo>())
                .Where(sheet => sheet != null)
                .ToList();

            return new PlotOutputPlan(
                allSheets.Where(sheet => sheet.Plotar).ToList(),
                allSheets.Where(sheet => sheet.GerarDwg).ToList(),
                allSheets.Where(sheet => sheet.Plotar || sheet.GerarDwg).ToList());
        }
    }
}
