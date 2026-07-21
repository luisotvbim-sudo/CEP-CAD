using System;
using System.Collections.Generic;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.ApplicationServices;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class SeloBlockCatalog
    {
        private readonly IZwcadContext _zwcad;
        private readonly BlockAttributeCatalog _attributes;

        public SeloBlockCatalog(
            IZwcadContext zwcad,
            BlockAttributeCatalog attributes)
        {
            _zwcad = zwcad ?? throw new ArgumentNullException(nameof(zwcad));
            _attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        }

        public IReadOnlyList<string> GetBlockNames()
        {
            Document document = _zwcad.ActiveDocument;
            if (document == null) return new List<string>();

            var names = new List<string>();
            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                var table = (BlockTable)transaction.GetObject(
                    document.Database.BlockTableId,
                    OpenMode.ForRead);

                foreach (ObjectId definitionId in table)
                {
                    BlockTableRecord definition = OpenDefinitionOrNull(
                        transaction,
                        definitionId);
                    if (IsSelectable(definition) && _attributes.HasAttributes(definition, transaction))
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

            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                var table = (BlockTable)transaction.GetObject(
                    document.Database.BlockTableId,
                    OpenMode.ForRead);
                if (!table.Has(blockName)) return new List<string>();

                var definition = (BlockTableRecord)transaction.GetObject(
                    table[blockName],
                    OpenMode.ForRead);
                IReadOnlyList<string> tags = _attributes.GetTags(definition, transaction);
                transaction.Commit();
                return tags;
            }
        }

        private static bool IsSelectable(BlockTableRecord definition)
        {
            return definition != null &&
                !definition.IsLayout &&
                !definition.IsAnonymous &&
                !definition.Name.StartsWith("*", StringComparison.Ordinal);
        }

        private static BlockTableRecord OpenDefinitionOrNull(
            Transaction transaction,
            ObjectId definitionId)
        {
            if (definitionId.IsNull || definitionId.IsErased) return null;

            try
            {
                return transaction.GetObject(
                    definitionId,
                    OpenMode.ForRead,
                    false) as BlockTableRecord;
            }
            catch
            {
                return null;
            }
        }
    }
}
