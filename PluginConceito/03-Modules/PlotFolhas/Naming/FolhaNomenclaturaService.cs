using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class FolhaNomenclaturaService
    {
        internal const string AttributeTag = "CNT_NOME_ARQUIVO";
        private readonly FolhaNameAttributeStore _attributeStore =
            new FolhaNameAttributeStore();

        public void LoadSavedNames(Document document, IEnumerable<FolhaInfo> sheets)
        {
            if (document == null || sheets == null)
            {
                return;
            }

            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                foreach (FolhaInfo sheet in sheets)
                {
                    if (sheet == null || sheet.BlockReferenceId.IsNull || sheet.BlockReferenceId.IsErased)
                    {
                        continue;
                    }

                    var block = transaction.GetObject(
                        sheet.BlockReferenceId,
                        OpenMode.ForRead,
                        false) as BlockReference;

                    if (block == null)
                    {
                        continue;
                    }

                    string savedName = _attributeStore.Read(block, transaction);
                    if (!string.IsNullOrWhiteSpace(savedName))
                    {
                        sheet.NomeArquivo = savedName.Trim();
                        string nameWithoutExtension = Path.GetFileNameWithoutExtension(sheet.NomeArquivo);
                        if (!string.IsNullOrWhiteSpace(nameWithoutExtension))
                            sheet.NomeArquivo = nameWithoutExtension + ".pdf";
                    }
                }

                transaction.Commit();
            }
        }

        public int SaveNames(Document document, IEnumerable<FolhaInfo> sheets)
        {
            if (document == null)
            {
                throw new InvalidOperationException("Nao existe desenho ativo.");
            }

            List<FolhaInfo> list = (sheets ?? Enumerable.Empty<FolhaInfo>())
                .Where(sheet => sheet != null)
                .ToList();

            if (list.Count == 0)
            {
                return 0;
            }

            int saved = 0;
            using (DocumentLock documentLock = document.LockDocument())
            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                foreach (FolhaInfo sheet in list)
                {
                    if (sheet.BlockReferenceId.IsNull || sheet.BlockReferenceId.IsErased)
                    {
                        continue;
                    }

                    var block = transaction.GetObject(
                        sheet.BlockReferenceId,
                        OpenMode.ForWrite,
                        false) as BlockReference;

                    if (block == null)
                    {
                        continue;
                    }

                    AttributeReference attribute = _attributeStore.GetOrCreate(block, transaction);
                    if (attribute == null)
                    {
                        continue;
                    }

                    if (!attribute.IsWriteEnabled)
                    {
                        attribute.UpgradeOpen();
                    }

                    attribute.Tag = AttributeTag;
                    attribute.TextString = Path.GetFileNameWithoutExtension(sheet.NomeArquivo ?? string.Empty);
                    attribute.Invisible = true;
                    saved++;
                }

                transaction.Commit();
            }

            return saved;
        }

    }
}
