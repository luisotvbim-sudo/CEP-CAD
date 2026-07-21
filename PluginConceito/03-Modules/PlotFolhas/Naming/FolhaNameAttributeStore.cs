using System;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class FolhaNameAttributeStore
    {
        public string Read(BlockReference block, Transaction transaction)
        {
            AttributeReference attribute = Find(
                block,
                transaction,
                OpenMode.ForRead);
            return attribute == null ? null : attribute.TextString;
        }

        public AttributeReference GetOrCreate(
            BlockReference block,
            Transaction transaction)
        {
            AttributeReference existing = Find(
                block,
                transaction,
                OpenMode.ForWrite);
            if (existing != null) return existing;

            EnsureDefinition(block, transaction);

            var attribute = new AttributeReference
            {
                Tag = FolhaNomenclaturaService.AttributeTag,
                TextString = string.Empty,
                Invisible = true,
                Position = block.Position,
                Height = 1.0
            };
            block.AttributeCollection.AppendAttribute(attribute);
            transaction.AddNewlyCreatedDBObject(attribute, true);
            return attribute;
        }

        private static void EnsureDefinition(
            BlockReference block,
            Transaction transaction)
        {
            ObjectId definitionId = block.IsDynamicBlock
                ? block.DynamicBlockTableRecord
                : block.BlockTableRecord;
            if (definitionId.IsNull || definitionId.IsErased) return;

            var definition = (BlockTableRecord)transaction.GetObject(
                definitionId,
                OpenMode.ForWrite);
            if (ContainsDefinition(definition, transaction)) return;

            var attribute = new AttributeDefinition
            {
                Tag = FolhaNomenclaturaService.AttributeTag,
                TextString = string.Empty,
                Invisible = true,
                Position = block.Position,
                Height = 1.0
            };
            definition.AppendEntity(attribute);
            transaction.AddNewlyCreatedDBObject(attribute, true);
        }

        private static bool ContainsDefinition(
            BlockTableRecord definition,
            Transaction transaction)
        {
            foreach (ObjectId entityId in definition)
            {
                if (entityId.IsNull || entityId.IsErased) continue;

                var attribute = transaction.GetObject(
                    entityId,
                    OpenMode.ForRead,
                    false) as AttributeDefinition;
                if (attribute != null && string.Equals(
                    attribute.Tag,
                    FolhaNomenclaturaService.AttributeTag,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static AttributeReference Find(
            BlockReference block,
            Transaction transaction,
            OpenMode openMode)
        {
            if (block == null || transaction == null) return null;

            foreach (ObjectId attributeId in block.AttributeCollection)
            {
                if (attributeId.IsNull || attributeId.IsErased) continue;

                var attribute = transaction.GetObject(
                    attributeId,
                    openMode,
                    false) as AttributeReference;
                if (attribute != null && string.Equals(
                    attribute.Tag,
                    FolhaNomenclaturaService.AttributeTag,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return attribute;
                }
            }

            return null;
        }
    }
}
