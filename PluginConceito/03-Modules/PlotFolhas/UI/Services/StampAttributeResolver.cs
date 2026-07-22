using System;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class StampAttributeResolver
    {
        private readonly BlockAttributeCatalog _attributes;
        private readonly StampBlockLocator _blocks;

        public StampAttributeResolver(
            BlockAttributeCatalog attributes,
            StampBlockLocator blocks)
        {
            _attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        }

        public StampAttributeMatch Find(
            BlockTableRecord paperSpace,
            Transaction transaction,
            FolhaInfo sheet,
            string blockName,
            string attributeTag)
        {
            if (paperSpace == null || transaction == null || sheet == null)
                return null;

            BlockReference block = _blocks.FindBestMatch(
                paperSpace,
                transaction,
                blockName,
                sheet.Limites);
            if (block == null) return null;

            AttributeReference attribute = _attributes.FindReference(
                block,
                transaction,
                attributeTag);
            return attribute == null
                ? null
                : new StampAttributeMatch(block, attribute);
        }
    }

    internal sealed class StampAttributeMatch
    {
        public StampAttributeMatch(
            BlockReference block,
            AttributeReference attribute)
        {
            Block = block ?? throw new ArgumentNullException(nameof(block));
            Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        }

        public BlockReference Block { get; }

        public AttributeReference Attribute { get; }
    }
}
