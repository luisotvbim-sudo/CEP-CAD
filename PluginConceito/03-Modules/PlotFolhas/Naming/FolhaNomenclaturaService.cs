using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class FolhaNomenclaturaService
    {
        internal const string AttributeTag = "CNT_NOME_ARQUIVO";

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

                    string savedName = ReadName(block, transaction);
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

                    AttributeReference attribute = GetOrCreateNameAttribute(block, transaction);
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

        private static string ReadName(BlockReference block, Transaction transaction)
        {
            AttributeReference attribute = FindNameAttribute(block, transaction, OpenMode.ForRead);
            return attribute == null ? null : attribute.TextString;
        }

        private static AttributeReference GetOrCreateNameAttribute(
            BlockReference block,
            Transaction transaction)
        {
            AttributeReference existing = FindNameAttribute(block, transaction, OpenMode.ForWrite);
            if (existing != null) return existing;

            EnsureAttributeDefinition(block, transaction);

            var attribute = new AttributeReference
            {
                Tag = AttributeTag,
                TextString = string.Empty,
                Invisible = true,
                Position = block.Position,
                Height = 1.0
            };

            block.AttributeCollection.AppendAttribute(attribute);
            transaction.AddNewlyCreatedDBObject(attribute, true);
            return attribute;
        }

        private static void EnsureAttributeDefinition(
            BlockReference block,
            Transaction transaction)
        {
            ObjectId definitionId = block.IsDynamicBlock
                ? block.DynamicBlockTableRecord
                : block.BlockTableRecord;

            if (definitionId.IsNull || definitionId.IsErased) return;

            BlockTableRecord definition = (BlockTableRecord)transaction.GetObject(
                definitionId, OpenMode.ForWrite);

            foreach (ObjectId id in definition)
            {
                if (id.IsNull || id.IsErased) continue;
                var attDef = transaction.GetObject(id, OpenMode.ForRead, false) as AttributeDefinition;
                if (attDef != null && string.Equals(
                    attDef.Tag, AttributeTag, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            var attributeDefinition = new AttributeDefinition
            {
                Tag = AttributeTag,
                TextString = string.Empty,
                Invisible = true,
                Position = block.Position,
                Height = 1.0
            };

            definition.AppendEntity(attributeDefinition);
            transaction.AddNewlyCreatedDBObject(attributeDefinition, true);
        }

        private static AttributeReference FindNameAttribute(
            BlockReference block,
            Transaction transaction,
            OpenMode openMode)
        {
            if (block == null || transaction == null) return null;

            foreach (ObjectId attributeId in block.AttributeCollection)
            {
                if (attributeId.IsNull || attributeId.IsErased) continue;

                var attribute = transaction.GetObject(attributeId, openMode, false) as AttributeReference;
                if (attribute == null) continue;

                if (string.Equals(attribute.Tag, AttributeTag, StringComparison.OrdinalIgnoreCase))
                    return attribute;
            }

            return null;
        }
    }
}
