using System;
using System.Collections.Generic;
using PluginConceito.Application.Contracts;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class SeloBlockService
    {
        private readonly SeloBlockCatalog _blockCatalog;
        private readonly SeloAttributeWriter _attributeWriter;
        private readonly SeloAttributeReader _attributeReader;

        public SeloBlockService(IZwcadContext zwcad)
        {
            if (zwcad == null) throw new ArgumentNullException(nameof(zwcad));

            var attributeCatalog = new BlockAttributeCatalog();
            var entitySpace = new SheetEntitySpace(zwcad);
            var attributeResolver = new StampAttributeResolver(
                attributeCatalog,
                new StampBlockLocator());
            _blockCatalog = new SeloBlockCatalog(zwcad, attributeCatalog);
            _attributeWriter = new SeloAttributeWriter(
                entitySpace,
                attributeResolver);
            _attributeReader = new SeloAttributeReader(
                entitySpace,
                attributeResolver);
        }

        public IReadOnlyList<string> GetBlockNames()
        {
            return _blockCatalog.GetBlockNames();
        }

        public IReadOnlyList<string> GetAttributeTags(string blockName)
        {
            return _blockCatalog.GetAttributeTags(blockName);
        }

        public int FillSeloAttributes(
            IReadOnlyList<FolhaInfo> sheets,
            string stampBlockName,
            string attributeTag)
        {
            return _attributeWriter.Fill(
                sheets,
                stampBlockName,
                attributeTag);
        }

        public int CopyAttributeValuesToSheetNames(
            IReadOnlyList<FolhaInfo> sheets,
            string stampBlockName,
            string attributeTag)
        {
            return _attributeReader.CopyToSheetNames(
                sheets,
                stampBlockName,
                attributeTag);
        }
    }
}
