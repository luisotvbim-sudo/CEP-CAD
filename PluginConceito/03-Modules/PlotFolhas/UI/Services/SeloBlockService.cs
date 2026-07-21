using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;
using ZwSoft.ZwCAD.Geometry;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class SeloBlockService
    {
        private readonly IZwcadContext _zwcad;

        public SeloBlockService(IZwcadContext zwcad)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
        }

        public IReadOnlyList<string> GetBlockNames()
        {
            Document document = _zwcad.ActiveDocument;
            if (document == null) return new List<string>();

            var names = new List<string>();
            Database database = document.Database;

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTable table = (BlockTable)transaction.GetObject(
                    database.BlockTableId, OpenMode.ForRead);

                foreach (ObjectId id in table)
                {
                    if (id.IsNull || id.IsErased) continue;

                    BlockTableRecord definition = (BlockTableRecord)transaction.GetObject(
                        id, OpenMode.ForRead);

                    if (definition == null) continue;
                    if (definition.IsLayout) continue;
                    if (definition.IsAnonymous) continue;
                    if (definition.Name.StartsWith("*", StringComparison.Ordinal)) continue;
                    if (!HasAttributes(definition)) continue;

                    names.Add(definition.Name);
                }

                transaction.Commit();
            }

            names.Sort(StringComparer.CurrentCultureIgnoreCase);
            return names;
        }

        public IReadOnlyList<string> GetAttributeTags(string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName)) return new List<string>();

            Document document = _zwcad.ActiveDocument;
            if (document == null) return new List<string>();

            var tags = new List<string>();
            Database database = document.Database;

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTable table = (BlockTable)transaction.GetObject(
                    database.BlockTableId, OpenMode.ForRead);

                if (!table.Has(blockName))
                {
                    transaction.Commit();
                    return tags;
                }

                ObjectId definitionId = table[blockName];
                BlockTableRecord definition = (BlockTableRecord)transaction.GetObject(
                    definitionId, OpenMode.ForRead);

                CollectAttributeTags(definition, transaction, tags);
                transaction.Commit();
            }

            tags.Sort(StringComparer.CurrentCultureIgnoreCase);
            return tags;
        }

        public int FillSeloAttributes(
            IReadOnlyList<FolhaInfo> sheets,
            string stampBlockName,
            string attributeTag)
        {
            if (sheets == null || sheets.Count == 0) return 0;
            if (string.IsNullOrWhiteSpace(stampBlockName)) return 0;
            if (string.IsNullOrWhiteSpace(attributeTag)) return 0;

            Document document = _zwcad.ActiveDocument;
            if (document == null) throw new InvalidOperationException("Nao existe desenho ativo.");

            string layoutName = LayoutManager.Current.CurrentLayout;
            if (string.Equals(layoutName, "Model", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("O comando deve ser executado em um layout.");

            int filled = 0;

            using (DocumentLock documentLock = document.LockDocument())
            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                ObjectId layoutId = LayoutManager.Current.GetLayoutId(layoutName);
                Layout layout = (Layout)transaction.GetObject(layoutId, OpenMode.ForRead);
                BlockTableRecord paperSpace = (BlockTableRecord)transaction.GetObject(
                    layout.BlockTableRecordId, OpenMode.ForRead);

                foreach (FolhaInfo sheet in sheets)
                {
                    if (sheet.BlockReferenceId.IsNull || sheet.BlockReferenceId.IsErased) continue;

                    BlockReference stampBlock = FindStampBlockInPaperSpace(
                        paperSpace, transaction, stampBlockName, sheet.Limites);

                    if (stampBlock == null) continue;

                    AttributeReference attribute = FindAttribute(
                        stampBlock, transaction, attributeTag);
                    if (attribute == null) continue;

                    if (!attribute.IsWriteEnabled) attribute.UpgradeOpen();
                    attribute.TextString = Path.GetFileNameWithoutExtension(sheet.NomeArquivo ?? string.Empty);
                    stampBlock.RecordGraphicsModified(true);
                    filled++;
                }

                transaction.Commit();
            }

            if (filled > 0)
            {
                document.SendStringToExecute(
                    "_.ATTSYNC\nN\n" + stampBlockName + "\n", true, false, false);
            }

            return filled;
        }

        private static BlockReference FindStampBlockInPaperSpace(
            BlockTableRecord paperSpace,
            Transaction transaction,
            string stampBlockName,
            Extents2d sheetBoundary)
        {
            BlockReference bestMatch = null;
            double bestOverlapArea = 0;

            foreach (ObjectId id in paperSpace)
            {
                if (id.IsNull || id.IsErased) continue;

                BlockReference block = transaction.GetObject(
                    id, OpenMode.ForRead, false) as BlockReference;
                if (block == null) continue;

                if (!string.Equals(
                    BlockNameHelper.GetEffectiveName(block, transaction),
                    stampBlockName,
                    StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    Extents3d extents = block.GeometricExtents;
                    double overlapWidth = Math.Min(sheetBoundary.MaxPoint.X, extents.MaxPoint.X) -
                        Math.Max(sheetBoundary.MinPoint.X, extents.MinPoint.X);
                    double overlapHeight = Math.Min(sheetBoundary.MaxPoint.Y, extents.MaxPoint.Y) -
                        Math.Max(sheetBoundary.MinPoint.Y, extents.MinPoint.Y);

                    if (overlapWidth <= 0 || overlapHeight <= 0) continue;

                    double overlapArea = overlapWidth * overlapHeight;
                    if (overlapArea > bestOverlapArea)
                    {
                        bestOverlapArea = overlapArea;
                        bestMatch = block;
                    }
                }
                catch { }
            }

            return bestMatch;
        }

        private static bool HasAttributes(BlockTableRecord definition)
        {
            foreach (ObjectId id in definition)
            {
                if (id.IsNull || id.IsErased) continue;
                try
                {
                    var obj = id.GetObject(OpenMode.ForRead, false);
                    if (obj is AttributeDefinition) return true;

                    var nestedBlock = obj as BlockReference;
                    if (nestedBlock == null) continue;

                    ObjectId nestedDefId = nestedBlock.IsDynamicBlock
                        ? nestedBlock.DynamicBlockTableRecord
                        : nestedBlock.BlockTableRecord;

                    if (nestedDefId.IsNull || nestedDefId.IsErased) continue;

                    var nestedDef = (BlockTableRecord)nestedDefId.GetObject(OpenMode.ForRead);
                    if (nestedDef != null && HasAttributes(nestedDef)) return true;
                }
                catch { }
            }

            return false;
        }

        private static void CollectAttributeTags(
            BlockTableRecord definition,
            Transaction transaction,
            List<string> tags)
        {
            foreach (ObjectId id in definition)
            {
                if (id.IsNull || id.IsErased) continue;

                AttributeDefinition attDef = transaction.GetObject(
                    id, OpenMode.ForRead, false) as AttributeDefinition;
                if (attDef != null && !string.IsNullOrWhiteSpace(attDef.Tag))
                {
                    if (!tags.Contains(attDef.Tag))
                        tags.Add(attDef.Tag);
                    continue;
                }

                BlockReference nestedBlock = transaction.GetObject(
                    id, OpenMode.ForRead, false) as BlockReference;
                if (nestedBlock == null) continue;

                ObjectId nestedDefId = nestedBlock.IsDynamicBlock
                    ? nestedBlock.DynamicBlockTableRecord
                    : nestedBlock.BlockTableRecord;

                if (nestedDefId.IsNull || nestedDefId.IsErased) continue;

                BlockTableRecord nestedDef = (BlockTableRecord)transaction.GetObject(
                    nestedDefId, OpenMode.ForRead);
                if (nestedDef != null)
                    CollectAttributeTags(nestedDef, transaction, tags);
            }
        }

        private static AttributeReference FindAttribute(
            BlockReference block,
            Transaction transaction,
            string tag)
        {
            foreach (ObjectId attributeId in block.AttributeCollection)
            {
                if (attributeId.IsNull || attributeId.IsErased) continue;

                AttributeReference attribute = (AttributeReference)transaction.GetObject(
                    attributeId, OpenMode.ForRead, false);
                if (attribute == null) continue;

                if (string.Equals(attribute.Tag, tag, StringComparison.OrdinalIgnoreCase))
                    return attribute;
            }

            return null;
        }

    }
}
