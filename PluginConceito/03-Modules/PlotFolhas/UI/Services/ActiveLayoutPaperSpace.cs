using System;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class ActiveLayoutPaperSpace
    {
        private readonly IZwcadContext _zwcad;

        public ActiveLayoutPaperSpace(IZwcadContext zwcad)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
        }

        public Document GetDocument()
        {
            Document document = _zwcad.ActiveDocument;
            if (document == null)
                throw new InvalidOperationException("Não existe desenho ativo.");

            if (string.Equals(
                LayoutManager.Current.CurrentLayout,
                "Model",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "O comando deve ser executado em um layout.");
            }

            return document;
        }

        public BlockTableRecord Open(Transaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            ObjectId layoutId = LayoutManager.Current.GetLayoutId(
                LayoutManager.Current.CurrentLayout);
            var layout = (Layout)transaction.GetObject(
                layoutId,
                OpenMode.ForRead);
            return (BlockTableRecord)transaction.GetObject(
                layout.BlockTableRecordId,
                OpenMode.ForRead);
        }
    }
}
