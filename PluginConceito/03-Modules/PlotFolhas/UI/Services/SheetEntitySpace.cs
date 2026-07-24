using System;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class SheetEntitySpace
    {
        private readonly IZwcadContext _zwcad;

        public SheetEntitySpace(IZwcadContext zwcad)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
        }

        public Document GetDocument()
        {
            Document document = _zwcad.ActiveDocument;
            if (document == null)
                throw new InvalidOperationException("Não existe desenho ativo.");

            return document;
        }

        public BlockTableRecord Open(
            Transaction transaction,
            FolhaInfo sheet)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));
            if (sheet == null)
                throw new ArgumentNullException(nameof(sheet));

            var layout = (Layout)transaction.GetObject(
                sheet.LayoutId,
                OpenMode.ForRead);
            return (BlockTableRecord)transaction.GetObject(
                layout.BlockTableRecordId,
                OpenMode.ForRead);
        }
    }
}
