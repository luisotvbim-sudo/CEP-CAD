using System;
using System.Collections.Generic;
using System.IO;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class SeloAttributeWriter
    {
        private readonly IZwcadContext _zwcad;
        private readonly BlockAttributeCatalog _attributes;
        private readonly StampBlockLocator _blockLocator;

        public SeloAttributeWriter(
            IZwcadContext zwcad,
            BlockAttributeCatalog attributes,
            StampBlockLocator blockLocator)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
            _attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
            _blockLocator = blockLocator ?? throw new ArgumentNullException(nameof(blockLocator));
        }

        public int Fill(
            IReadOnlyList<FolhaInfo> sheets,
            string blockName,
            string attributeTag)
        {
            if (!HasConfiguration(sheets, blockName, attributeTag)) return 0;

            Document document = GetActiveLayoutDocument();
            int filled = FillAttributes(
                document,
                sheets,
                blockName,
                attributeTag);

            if (filled > 0) SynchronizeAttributes(document, blockName);
            return filled;
        }

        private int FillAttributes(
            Document document,
            IEnumerable<FolhaInfo> sheets,
            string blockName,
            string attributeTag)
        {
            int filled = 0;

            using (DocumentLock documentLock = document.LockDocument())
            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                BlockTableRecord paperSpace = OpenCurrentPaperSpace(transaction);

                foreach (FolhaInfo sheet in sheets)
                {
                    if (sheet == null ||
                        sheet.BlockReferenceId.IsNull ||
                        sheet.BlockReferenceId.IsErased)
                    {
                        continue;
                    }

                    BlockReference block = _blockLocator.FindBestMatch(
                        paperSpace,
                        transaction,
                        blockName,
                        sheet.Limites);
                    AttributeReference attribute = block == null
                        ? null
                        : _attributes.FindReference(block, transaction, attributeTag);
                    if (attribute == null) continue;

                    WriteFileName(attribute, block, sheet.NomeArquivo);
                    filled++;
                }

                transaction.Commit();
            }

            return filled;
        }

        private Document GetActiveLayoutDocument()
        {
            Document document = _zwcad.ActiveDocument;
            if (document == null)
                throw new InvalidOperationException("Não existe desenho ativo.");
            if (string.Equals(
                LayoutManager.Current.CurrentLayout,
                "Model",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("O comando deve ser executado em um layout.");
            }

            return document;
        }

        private static BlockTableRecord OpenCurrentPaperSpace(Transaction transaction)
        {
            ObjectId layoutId = LayoutManager.Current.GetLayoutId(
                LayoutManager.Current.CurrentLayout);
            var layout = (Layout)transaction.GetObject(layoutId, OpenMode.ForRead);
            return (BlockTableRecord)transaction.GetObject(
                layout.BlockTableRecordId,
                OpenMode.ForRead);
        }

        private static void WriteFileName(
            AttributeReference attribute,
            BlockReference block,
            string fileName)
        {
            if (!attribute.IsWriteEnabled) attribute.UpgradeOpen();
            attribute.TextString = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
            block.RecordGraphicsModified(true);
        }

        private static void SynchronizeAttributes(Document document, string blockName)
        {
            document.SendStringToExecute(
                "_.ATTSYNC\nN\n" + blockName + "\n",
                true,
                false,
                false);
        }

        private static bool HasConfiguration(
            IReadOnlyList<FolhaInfo> sheets,
            string blockName,
            string attributeTag)
        {
            return sheets != null && sheets.Count > 0 &&
                !string.IsNullOrWhiteSpace(blockName) &&
                !string.IsNullOrWhiteSpace(attributeTag);
        }
    }
}
