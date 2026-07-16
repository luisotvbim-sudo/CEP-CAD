using System;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.PlottingServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal static class PlotSettingsConfigurator
    {
        public static void Configure(PlotSettings settings, FolhaInfo sheet, string deviceName, string ctbName)
        {
            PlotSettingsValidator validator = PlotSettingsValidator.Current;
            validator.SetPlotConfigurationName(settings, deviceName, null);
            validator.RefreshLists(settings);
            validator.SetPlotPaperUnits(settings, PlotPaperUnit.Millimeters);
            validator.SetPlotType(settings, ZwSoft.ZwCAD.DatabaseServices.PlotType.Window);
            validator.SetPlotWindowArea(settings, sheet.Limites);
            validator.SetPlotCentered(settings, true);
            validator.SetUseStandardScale(settings, true);
            validator.SetStdScaleType(settings, StdScaleType.StdScale1To1);
            validator.SetClosestMediaName(
                settings,
                Math.Abs(sheet.Largura),
                Math.Abs(sheet.Altura),
                PlotPaperUnit.Millimeters,
                true);
            validator.SetPlotRotation(settings, PlotRotation.Degrees000);

            settings.PrintLineweights = true;
            settings.PlotPlotStyles = !string.IsNullOrWhiteSpace(ctbName);
            settings.ShowPlotStyles = !string.IsNullOrWhiteSpace(ctbName);
            if (!string.IsNullOrWhiteSpace(ctbName)) validator.SetCurrentStyleSheet(settings, ctbName);
        }
    }
}
