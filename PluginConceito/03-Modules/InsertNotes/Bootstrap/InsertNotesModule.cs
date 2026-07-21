using System;
using PluginConceito.Application.Contracts;

namespace PluginConceito.Modules.InsertNotes
{
    public sealed class InsertNotesModule : ICntModule
    {
        public string Id
        {
            get { return "InsertNotes"; }
        }

        internal static InsertNotesHandler Handler { get; private set; }

        public void Initialize(IModuleContext context)
        {
            Handler = new InsertNotesHandler(context);
        }

        internal static void Execute()
        {
            InsertNotesHandler handler = Handler;
            if (handler == null)
            {
                throw new InvalidOperationException("O módulo InsertNotes ainda não foi inicializado.");
            }

            handler.Execute();
        }
    }
}
