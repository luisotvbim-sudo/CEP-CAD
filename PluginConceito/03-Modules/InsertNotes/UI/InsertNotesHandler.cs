using System;
using PluginConceito.Application.Contracts;

namespace PluginConceito.Modules.InsertNotes
{
    internal sealed class InsertNotesHandler
    {
        private readonly IModuleContext _context;

        public InsertNotesHandler(IModuleContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Execute()
        {
            try
            {
                if (_context.Zwcad.ActiveDocument == null)
                {
                    throw new InvalidOperationException("Não existe desenho ativo.");
                }

                var viewModel = new InsertNotesViewModel();
                var window = new InsertNotesWindow(viewModel);
                window.Show();

                _context.Telemetry.TrackEvent("CNT_INSERT_NOTES.Success");
            }
            catch (Exception exception)
            {
                _context.Telemetry.TrackException("CNT_INSERT_NOTES.Execute", exception);
                throw;
            }
        }
    }
}
