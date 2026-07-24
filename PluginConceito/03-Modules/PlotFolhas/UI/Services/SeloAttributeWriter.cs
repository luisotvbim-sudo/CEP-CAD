using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class SeloAttributeWriter
    {
        private readonly SheetEntitySpace _entitySpace;
        private readonly StampAttributeResolver _attributeResolver;

        public SeloAttributeWriter(
            SheetEntitySpace entitySpace,
            StampAttributeResolver attributeResolver)
        {
            _entitySpace = entitySpace ?? throw new ArgumentNullException(nameof(entitySpace));
            _attributeResolver = attributeResolver ??
                throw new ArgumentNullException(nameof(attributeResolver));
        }

        public int Fill(
            IReadOnlyList<FolhaInfo> sheets,
            string blockName,
            string attributeTag)
        {
            if (!HasConfiguration(sheets, blockName, attributeTag)) return 0;

            Document document = _entitySpace.GetDocument();
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
                    if (sheet == null ||
                        sheet.BlockReferenceId.IsNull ||
                        sheet.BlockReferenceId.IsErased)
                    {
                        continue;
                    }

                    StampAttributeMatch match = _attributeResolver.Find(
                        entitySpace,
                        transaction,
                        sheet,
                        blockName,
                        attributeTag);
                    if (match == null) continue;

                    WriteFileName(match.Attribute, match.Block, sheet.NomeArquivo);
                    filled++;
                }

                transaction.Commit();
            }

            return filled;
        }

        private static void WriteFileName(
            AttributeReference attribute,
            BlockReference block,
            string fileName)
        {
            if (!attribute.IsWriteEnabled) attribute.UpgradeOpen();
            attribute.TextString = Path.GetFileNameWithoutExtension(
                fileName ?? string.Empty);
            block.RecordGraphicsModified(true);
        }

        private static void SynchronizeAttributes(
            Document document,
            string blockName)
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
