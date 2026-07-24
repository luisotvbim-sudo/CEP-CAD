using System;
using System.Collections.Generic;
using System.Linq;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class SeloAttributeReader
    {
        private readonly SheetEntitySpace _entitySpace;
        private readonly StampAttributeResolver _attributeResolver;

        public SeloAttributeReader(
            SheetEntitySpace entitySpace,
            StampAttributeResolver attributeResolver)
        {
            _entitySpace = entitySpace ?? throw new ArgumentNullException(nameof(entitySpace));
            _attributeResolver = attributeResolver ??
                throw new ArgumentNullException(nameof(attributeResolver));
        }

        public int CopyToSheetNames(
            IReadOnlyList<FolhaInfo> sheets,
            string blockName,
            string attributeTag)
        {
            if (!HasConfiguration(sheets, blockName, attributeTag)) return 0;

            Document document = _entitySpace.GetDocument();
            int copied = 0;

            using (DocumentLock documentLock = document.LockDocument())
            using (Transaction transaction =
                document.Database.TransactionManager.StartTransaction())
            {
                FolhaInfo firstSheet = sheets.FirstOrDefault(
                    sheet => sheet != null);
                if (firstSheet == null)
                {
                    return 0;
                }

                BlockTableRecord entitySpace = _entitySpace.Open(
                    transaction,
                    firstSheet);

                foreach (FolhaInfo sheet in sheets)
                {
                    if (!CanRead(sheet)) continue;

                    StampAttributeMatch match = _attributeResolver.Find(
                        entitySpace,
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
