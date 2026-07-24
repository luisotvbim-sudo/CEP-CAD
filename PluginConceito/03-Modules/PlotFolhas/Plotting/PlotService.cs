using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.PlottingServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class PlotService
    {
        private readonly IZwcadContext _zwcad;

        public PlotService(IZwcadContext zwcad)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
        }

        public IReadOnlyList<string> GetPlotDevices()
        {
            return ToSortedList(PlotSettingsValidator.Current.GetPlotDeviceList());
        }

        public IReadOnlyList<string> GetPlotStyleSheets()
        {
            List<string> styles = ToSortedList(PlotSettingsValidator.Current.GetPlotStyleSheetList()).ToList();
            styles.Insert(0, string.Empty);
            return styles;
        }

        public string GetDefaultPlotDevice(IReadOnlyList<string> devices)
        {
            if (devices == null || devices.Count == 0)
            {
                return string.Empty;
            }

            string pdf = devices.FirstOrDefault(item =>
                item.IndexOf("PDF", StringComparison.OrdinalIgnoreCase) >= 0);
            if (!string.IsNullOrWhiteSpace(pdf))
            {
                return pdf;
            }

            string current = GetCurrentPlotDevice();
            if (!string.IsNullOrWhiteSpace(current) &&
                devices.Any(item => string.Equals(item, current, StringComparison.OrdinalIgnoreCase)))
            {
                return current;
            }

            return devices[0];
        }

        public string GetDefaultPlotStyle(IReadOnlyList<string> styles)
        {
            if (styles == null || styles.Count == 0)
            {
                return string.Empty;
            }

            string current = GetCurrentPlotStyle();
            if (!string.IsNullOrWhiteSpace(current) &&
                styles.Any(item => string.Equals(item, current, StringComparison.OrdinalIgnoreCase)))
            {
                return current;
            }

            string monochrome = styles.FirstOrDefault(item =>
                item.IndexOf("monochrome", StringComparison.OrdinalIgnoreCase) >= 0);
            if (!string.IsNullOrWhiteSpace(monochrome))
            {
                return monochrome;
            }

            return styles[0];
        }

        public int PlotSheets(
            IReadOnlyList<FolhaInfo> sheets,
            string outputFolder,
            string deviceName,
            string ctbName,
            bool overwriteExisting,
            Action<string> progress)
        {
            if (sheets == null || sheets.Count == 0)
            {
                return 0;
            }

            Document document = _zwcad.ActiveDocument;
            if (document == null)
            {
                throw new InvalidOperationException("Nao existe desenho ativo.");
            }

            if (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)
            {
                throw new InvalidOperationException("Ja existe uma plotagem em andamento no ZWCAD.");
            }

            if (!IsPdfPlotDevice(deviceName))
            {
                throw new InvalidOperationException(
                    "Escolha um plotter PDF. Plotter selecionado: " + deviceName);
            }

            Report(progress, string.Format(
                "Plot inicio: {0} folha(s), device={1}, estilo={2}, pasta={3}",
                sheets.Count,
                deviceName,
                string.IsNullOrWhiteSpace(ctbName) ? "(sem CTB/STB)" : ctbName,
                outputFolder));

            int plotted = 0;
            using (DocumentLock documentLock = document.LockDocument())
            {
                for (int index = 0; index < sheets.Count; index++)
                {
                    FolhaInfo sheet = sheets[index];
                    string outputPath = Path.Combine(outputFolder, sheet.NomeArquivo);

                    Report(progress, string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Plot folha {0}/{1}: seq={2:00}, formato={3}, arquivo={4}, LL={5:0.###},{6:0.###}, UR={7:0.###},{8:0.###}",
                        index + 1,
                        sheets.Count,
                        sheet.Sequencia,
                        sheet.Formato,
                        outputPath,
                        sheet.Limites.MinPoint.X,
                        sheet.Limites.MinPoint.Y,
                        sheet.Limites.MaxPoint.X,
                        sheet.Limites.MaxPoint.Y));

                    if (File.Exists(outputPath))
                    {
                        if (!overwriteExisting)
                        {
                            throw new IOException("Arquivo ja existe: " + outputPath);
                        }

                        Report(progress, "Plot folha " + sheet.Sequencia + ": removendo PDF existente.");
                        File.Delete(outputPath);
                    }

                    using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
                    {
                        PlotSingleSheet(document, transaction, sheet, outputPath, deviceName, ctbName, progress);
                        transaction.Commit();
                    }

                    if (!File.Exists(outputPath))
                    {
                        throw new IOException(
                            "A plotagem terminou, mas o PDF nao foi encontrado: " + outputPath);
                    }

                    Report(progress, "Plot folha " + sheet.Sequencia + ": PDF gerado.");
                    plotted++;
                }
            }

            Report(progress, "Plot concluido: " + plotted + " PDF(s).");
            return plotted;
        }

        private void PlotSingleSheet(
            Document document,
            Transaction transaction,
            FolhaInfo sheet,
            string outputPath,
            string deviceName,
            string ctbName,
            Action<string> progress)
        {
            LayoutManager.Current.CurrentLayout = sheet.LayoutName;
            Report(
                progress,
                "Plot folha " + sheet.Sequencia +
                ": espaço de origem ativo definido como " +
                sheet.LayoutName + ".");

            var layout = (Layout)transaction.GetObject(sheet.LayoutId, OpenMode.ForRead);
            using (var settings = new PlotSettings(layout.ModelType))
            {
                settings.CopyFrom(layout);
                Report(progress, "Plot folha " + sheet.Sequencia + ": configurando PlotSettings.");
                PlotSettingsConfigurator.Configure(settings, sheet, deviceName, ctbName);
                Report(progress,
                    "Plot folha " + sheet.Sequencia +
                    ": media=" + settings.CanonicalMediaName +
                    ", device=" + settings.PlotConfigurationName +
                    ", estilo=" + settings.CurrentStyleSheet);

                using (var plotInfo = new PlotInfo())
                {
                    plotInfo.Layout = sheet.LayoutId;
                    plotInfo.OverrideSettings = settings;

                    using (var validator = new PlotInfoValidator())
                    {
                        Report(progress, "Plot folha " + sheet.Sequencia + ": validando PlotInfo.");
                        validator.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                        validator.Validate(plotInfo);
                        Report(progress, "Plot folha " + sheet.Sequencia + ": PlotInfo validado.");
                    }

                    Publish(document, plotInfo, outputPath, sheet, progress);
                }
            }
        }

        private void Publish(
            Document document,
            PlotInfo plotInfo,
            string outputPath,
            FolhaInfo sheet,
            Action<string> progress)
        {
            Report(progress, "Plot folha " + sheet.Sequencia + ": criando engine.");
            using (PlotEngine engine = PlotFactory.CreatePublishEngine())
            {
                using (var plotProgress = new PlotProgressDialog(false, 1, false))
                {
                    plotProgress.set_PlotMsgString(PlotMessageIndex.DialogTitle, "CNT - Plotar folhas");
                    plotProgress.set_PlotMsgString(
                        PlotMessageIndex.SheetName,
                        "Folha " + sheet.Sequencia + " - " + sheet.Formato);
                    plotProgress.set_PlotMsgString(PlotMessageIndex.Status, "Gerando PDF...");
                    plotProgress.LowerPlotProgressRange = 0;
                    plotProgress.UpperPlotProgressRange = 100;
                    plotProgress.PlotProgressPos = 0;
                    plotProgress.IsVisible = false;

                    Report(progress, "Plot folha " + sheet.Sequencia + ": BeginPlot.");
                    plotProgress.OnBeginPlot();
                    engine.BeginPlot(plotProgress, null);

                    Report(progress, "Plot folha " + sheet.Sequencia + ": BeginDocument.");
                    engine.BeginDocument(plotInfo, document.Name, null, 1, true, outputPath);

                    using (var pageInfo = new PlotPageInfo())
                    {
                        Report(progress, "Plot folha " + sheet.Sequencia + ": BeginPage.");
                        plotProgress.OnBeginSheet();
                        engine.BeginPage(pageInfo, plotInfo, true, null);

                        Report(progress, "Plot folha " + sheet.Sequencia + ": BeginGenerateGraphics.");
                        engine.BeginGenerateGraphics(null);

                        Report(progress, "Plot folha " + sheet.Sequencia + ": EndGenerateGraphics.");
                        engine.EndGenerateGraphics(null);

                        Report(progress, "Plot folha " + sheet.Sequencia + ": EndPage.");
                        engine.EndPage(null);
                        plotProgress.OnEndSheet();
                    }

                    Report(progress, "Plot folha " + sheet.Sequencia + ": EndDocument.");
                    engine.EndDocument(null);
                    plotProgress.PlotProgressPos = 100;

                    Report(progress, "Plot folha " + sheet.Sequencia + ": EndPlot.");
                    engine.EndPlot(null);
                    plotProgress.OnEndPlot();
                }
            }
        }

        private void Report(Action<string> progress, string message)
        {
            _zwcad.WriteMessage(message);
            if (progress != null)
            {
                progress(message);
            }
        }

        private string GetCurrentPlotDevice()
        {
            return GetCurrentLayoutValue(layout => layout.PlotConfigurationName);
        }

        private string GetCurrentPlotStyle()
        {
            return GetCurrentLayoutValue(layout => layout.CurrentStyleSheet);
        }

        private string GetCurrentLayoutValue(Func<Layout, string> selector)
        {
            Document document = _zwcad.ActiveDocument;
            if (document == null)
            {
                return null;
            }

            try
            {
                using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
                {
                    ObjectId layoutId = LayoutManager.Current.GetLayoutId(LayoutManager.Current.CurrentLayout);
                    var layout = (Layout)transaction.GetObject(layoutId, OpenMode.ForRead);
                    string value = selector(layout);
                    transaction.Commit();
                    return value;
                }
            }
            catch
            {
                return null;
            }
        }

        private static bool IsPdfPlotDevice(string deviceName)
        {
            return !string.IsNullOrWhiteSpace(deviceName) &&
                deviceName.IndexOf("PDF", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IReadOnlyList<string> ToSortedList(StringCollection values)
        {
            if (values == null)
            {
                return new List<string>();
            }

            return values
                .Cast<string>()
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
    }
}
