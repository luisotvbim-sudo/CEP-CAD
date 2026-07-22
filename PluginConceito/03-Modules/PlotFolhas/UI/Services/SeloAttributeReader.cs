using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class SeloAttributeReader
    {
        private readonly ActiveLayoutPaperSpace _paperSpace;
        private readonly StampAttributeResolver _attributeResolver;

        public SeloAttributeReader(
            ActiveLayoutPaperSpace paperSpace,
            StampAttributeResolver attributeResolver)
        {
            _paperSpace = paperSpace ?? throw new ArgumentNullException(nameof(paperSpace));
            _attributeResolver = attributeResolver ??
                throw new ArgumentNullException(nameof(attributeResolver));
        }

        public int CopyToSheetNames(
            IReadOnlyList<FolhaInfo> sheets,
            string blockName,
            string attributeTag)
        {
            if (!HasConfiguration(sheets, blockName, attributeTag)) return 0;

            Document document = _paperSpace.GetDocument();
            int copied = 0;

            using (DocumentLock documentLock = document.LockDocument())
            using (Transaction transaction =
                document.Database.TransactionManager.StartTransaction())
            {
                BlockTableRecord paperSpace = _paperSpace.Open(transaction);

                foreach (FolhaInfo sheet in sheets)
                {
                    if (!CanRead(sheet)) continue;

                    StampAttributeMatch match = _attributeResolver.Find(
                        paperSpace,
                        transaction,
                        sheet,
                        blockName,
                        attributeTag);
                    string value = match?.Attribute.TextString;
                    if (string.IsNullOrWhiteSpace(value)) continue;

                    sheet.NomeArquivo = value.Trim();
                    copied++;
                }

                transaction.Commit();
            }

            return copied;
        }

        private static bool CanRead(FolhaInfo sheet)
        {
            return sheet != null &&
                !sheet.BlockReferenceId.IsNull &&
                !sheet.BlockReferenceId.IsErased;
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
