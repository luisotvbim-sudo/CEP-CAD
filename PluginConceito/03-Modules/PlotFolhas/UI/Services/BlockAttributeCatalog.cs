using System;
using System.Collections.Generic;
using ZwSoft.ZwCAD.DatabaseServices;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class BlockAttributeCatalog
    {
        public bool HasAttributes(
            BlockTableRecord definition,
            Transaction transaction)
        {
            return HasAttributes(
                definition,
                transaction,
                new HashSet<ObjectId>());
        }

        public IReadOnlyList<string> GetTags(
            BlockTableRecord definition,
            Transaction transaction)
        {
            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectTags(
                definition,
                transaction,
                tags,
                new HashSet<ObjectId>());

            var result = new List<string>(tags);
            result.Sort(StringComparer.CurrentCultureIgnoreCase);
            return result;
        }

        public AttributeReference FindReference(
            BlockReference block,
            Transaction transaction,
            string tag)
        {
            foreach (ObjectId attributeId in block.AttributeCollection)
            {
                if (attributeId.IsNull || attributeId.IsErased) continue;

                var attribute = transaction.GetObject(
                    attributeId,
                    OpenMode.ForRead,
                    false) as AttributeReference;
                if (attribute != null && string.Equals(
                    attribute.Tag,
                    tag,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return attribute;
                }
            }

            return null;
        }

        private static bool HasAttributes(
            BlockTableRecord definition,
            Transaction transaction,
            ISet<ObjectId> visitedDefinitions)
        {
            if (!MarkVisited(definition, visitedDefinitions)) return false;

            foreach (ObjectId entityId in definition)
            {
                DBObject entity = OpenOrNull(transaction, entityId);
                if (entity is AttributeDefinition) return true;

                BlockTableRecord nested = OpenNestedDefinitionOrNull(
                    entity as BlockReference,
                    transaction);
                if (nested != null && HasAttributes(nested, transaction, visitedDefinitions))
                    return true;
            }

            return false;
        }

        private static void CollectTags(
            BlockTableRecord definition,
            Transaction transaction,
            ISet<string> tags,
            ISet<ObjectId> visitedDefinitions)
        {
            if (!MarkVisited(definition, visitedDefinitions)) return;

            foreach (ObjectId entityId in definition)
            {
                DBObject entity = OpenOrNull(transaction, entityId);
                var attribute = entity as AttributeDefinition;
                if (attribute != null && !string.IsNullOrWhiteSpace(attribute.Tag))
                {
                    tags.Add(attribute.Tag);
                    continue;
                }

                BlockTableRecord nested = OpenNestedDefinitionOrNull(
                    entity as BlockReference,
                    transaction);
                if (nested != null)
                    CollectTags(nested, transaction, tags, visitedDefinitions);
            }
        }

        private static bool MarkVisited(
            BlockTableRecord definition,
            ISet<ObjectId> visitedDefinitions)
        {
            return definition != null &&
                (definition.ObjectId.IsNull || visitedDefinitions.Add(definition.ObjectId));
        }

        private static DBObject OpenOrNull(
            Transaction transaction,
            ObjectId entityId)
        {
            if (entityId.IsNull || entityId.IsErased) return null;

            try
            {
                return transaction.GetObject(entityId, OpenMode.ForRead, false);
            }
            catch
            {
                return null;
            }
        }

        private static BlockTableRecord OpenNestedDefinitionOrNull(
            BlockReference block,
            Transaction transaction)
        {
            if (block == null) return null;

            ObjectId definitionId = block.IsDynamicBlock
                ? block.DynamicBlockTableRecord
                : block.BlockTableRecord;
            return OpenOrNull(transaction, definitionId) as BlockTableRecord;
        }
    }
}
