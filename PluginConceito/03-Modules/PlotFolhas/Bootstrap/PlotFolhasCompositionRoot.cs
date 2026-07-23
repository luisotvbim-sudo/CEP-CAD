using System;
using PluginConceito.Application.Contracts;

namespace PluginConceito.Modules.PlotFolhas
{
    internal static class PlotFolhasCompositionRoot
    {
        public static PlotFolhasHandler Create(IModuleContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var formats = new FolhaFormatCatalog();
            var fileNames = new ArquivoNomeService();
            var nomenclature = new FolhaNomenclaturaService();
            var plotService = new PlotService(context.Zwcad);
            var namingService = new PlotFolhasNamingService(
                context.Zwcad,
                fileNames,
                nomenclature);
            var stampService = new SeloBlockService(context.Zwcad);
            var generationService = CreateGenerationService(
                context,
                formats,
                nomenclature,
                plotService);

            var sessionService = new PlotFolhasSessionService(
                context.Zwcad,
                new FolhaScanner(
                    context.Zwcad,
                    formats,
                    new FolhaBoundaryResolver(),
                    new FolhaValidationService(formats)),
                fileNames,
                nomenclature,
                plotService,
                new NamingStandardParser());
            var namingWorkflow = new PlotFolhasNamingWorkflow(
                namingService,
                stampService,
                context.Telemetry);
            var zoomWorkflow = new PlotFolhasZoomWorkflow(
                new SheetZoomService(context.Zwcad),
                context.Telemetry);
            var generationWorkflow = new PlotFolhasGenerationWorkflow(
                generationService,
                namingService,
                new PlotFolhasGenerationRunner(
                    generationService,
                    stampService,
                    context.Telemetry));

            return new PlotFolhasHandler(
                context,
                sessionService,
                namingWorkflow,
                generationWorkflow,
                zoomWorkflow,
                new PlotFolhasDocumentTracker());
        }

        private static PlotFolhasGenerationService CreateGenerationService(
            IModuleContext context,
            FolhaFormatCatalog formats,
            FolhaNomenclaturaService nomenclature,
            PlotService plotService)
        {
            var execution = new PlotExecutionService(
                context.Zwcad,
                nomenclature,
                plotService,
                new DwgExportService(context.Zwcad, formats));
            return new PlotFolhasGenerationService(
                execution,
                new OutputFolderService());
        }
    }
}
