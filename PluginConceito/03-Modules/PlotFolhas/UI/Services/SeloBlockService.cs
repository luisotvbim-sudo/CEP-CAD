using System;
using System.Collections.Generic;
using PluginConceito.Application.Contracts;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class SeloBlockService
    {
        private readonly SeloBlockCatalog _blockCatalog;
        private readonly SeloAttributeWriter _attributeWriter;

        public SeloBlockService(IZwcadContext zwcad)
        {
            if (zwcad == null) throw new ArgumentNullException(nameof(zwcad));

            var attributeCatalog = new BlockAttributeCatalog();
            _blockCatalog = new SeloBlockCatalog(zwcad, attributeCatalog);
            _attributeWriter = new SeloAttributeWriter(
                zwcad,
                attributeCatalog,
                new StampBlockLocator());
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
    }
}
