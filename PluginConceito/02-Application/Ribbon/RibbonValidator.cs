using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ZwSoft.Windows;

namespace PluginConceito.Application.Ribbon
{
    public sealed class RibbonValidator
    {
        public RibbonValidationResult Validate(
            Assembly assembly,
            IEnumerable<RibbonItemDefinition> definitions)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            var items = definitions.ToList();
            var invalid = new HashSet<RibbonItemDefinition>();
            var errors = new List<string>();

            foreach (RibbonItemDefinition definition in items)
            {
                ValidateDefinition(assembly, definition, invalid, errors);
            }

            MarkDuplicates(
                items,
                item => item.RibbonAttribute.CommandName,
                "CommandName",
                invalid,
                errors);

            MarkDuplicates(
                items,
                item => item.RibbonAttribute.ButtonId,
                "ButtonId",
                invalid,
                errors);

            MarkInconsistentTitles(
                items,
                item => item.RibbonAttribute.TabId,
                item => item.RibbonAttribute.TabTitle,
                "aba",
                invalid,
                errors);

            MarkInconsistentTitles(
                items,
                item => item.RibbonAttribute.TabId + "\u001f" + item.RibbonAttribute.PanelId,
                item => item.RibbonAttribute.PanelTitle,
                "painel",
                invalid,
                errors);

            return new RibbonValidationResult(
                items.Where(item => !invalid.Contains(item)).ToList(),
                errors);
        }

        private static void ValidateDefinition(
            Assembly assembly,
            RibbonItemDefinition definition,
            ISet<RibbonItemDefinition> invalid,
            ICollection<string> errors)
        {
            string source = GetSource(definition);
            var attribute = definition.RibbonAttribute;

            Require(attribute.CommandName, "CommandName", source, definition, invalid, errors);
            Require(attribute.ButtonId, "ButtonId", source, definition, invalid, errors);
            Require(attribute.DisplayName, "DisplayName", source, definition, invalid, errors);
            Require(attribute.TabId, "TabId", source, definition, invalid, errors);
            Require(attribute.TabTitle, "TabTitle", source, definition, invalid, errors);
            Require(attribute.PanelId, "PanelId", source, definition, invalid, errors);
            Require(attribute.PanelTitle, "PanelTitle", source, definition, invalid, errors);

            if (definition.CommandAttribute == null)
            {
                AddError(definition, source + ": o método não possui CommandMethod.", invalid, errors);
            }
            else if (!string.Equals(
                definition.CommandAttribute.GlobalName,
                attribute.CommandName,
                StringComparison.OrdinalIgnoreCase))
            {
                AddError(
                    definition,
                    source + ": CommandMethod e CntRibbonCommand usam nomes diferentes.",
                    invalid,
                    errors);
            }

            if (!definition.Method.IsPublic)
            {
                AddError(definition, source + ": o método de comando deve ser público.", invalid, errors);
            }

            if (definition.Method.ReturnType != typeof(void) || definition.Method.GetParameters().Length != 0)
            {
                AddError(
                    definition,
                    source + ": o método de comando deve retornar void e não receber parâmetros.",
                    invalid,
                    errors);
            }

            if (!Enum.IsDefined(typeof(RibbonItemSize), attribute.Size))
            {
                AddError(definition, source + ": tamanho de item não suportado.", invalid, errors);
            }

            if (!string.IsNullOrWhiteSpace(attribute.IconResource) &&
                !RibbonIconLoader.ResourceExists(assembly, attribute.IconResource))
            {
                AddError(
                    definition,
                    source + ": recurso de ícone não encontrado: " + attribute.IconResource + ".",
                    invalid,
                    errors);
            }
        }

        private static void Require(
            string value,
            string field,
            string source,
            RibbonItemDefinition definition,
            ISet<RibbonItemDefinition> invalid,
            ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                AddError(definition, source + ": " + field + " não pode ficar vazio.", invalid, errors);
            }
        }

        private static void MarkDuplicates(
            IEnumerable<RibbonItemDefinition> items,
            Func<RibbonItemDefinition, string> selector,
            string field,
            ISet<RibbonItemDefinition> invalid,
            ICollection<string> errors)
        {
            var duplicateGroups = items
                .Where(item => !string.IsNullOrWhiteSpace(selector(item)))
                .GroupBy(selector, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1);

            foreach (var group in duplicateGroups)
            {
                foreach (RibbonItemDefinition definition in group)
                {
                    AddError(
                        definition,
                        GetSource(definition) + ": " + field + " duplicado: " + group.Key + ".",
                        invalid,
                        errors);
                }
            }
        }

        private static void MarkInconsistentTitles(
            IEnumerable<RibbonItemDefinition> items,
            Func<RibbonItemDefinition, string> idSelector,
            Func<RibbonItemDefinition, string> titleSelector,
            string kind,
            ISet<RibbonItemDefinition> invalid,
            ICollection<string> errors)
        {
            var inconsistentGroups = items
                .Where(item => !string.IsNullOrWhiteSpace(idSelector(item)))
                .GroupBy(idSelector, StringComparer.OrdinalIgnoreCase)
                .Where(group => group
                    .Select(titleSelector)
                    .Where(title => !string.IsNullOrWhiteSpace(title))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() > 1);

            foreach (var group in inconsistentGroups)
            {
                foreach (RibbonItemDefinition definition in group)
                {
                    AddError(
                        definition,
                        GetSource(definition) + ": o mesmo ID de " + kind + " possui títulos diferentes.",
                        invalid,
                        errors);
                }
            }
        }

        private static void AddError(
            RibbonItemDefinition definition,
            string message,
            ISet<RibbonItemDefinition> invalid,
            ICollection<string> errors)
        {
            invalid.Add(definition);
            errors.Add(message);
        }

        private static string GetSource(RibbonItemDefinition definition)
        {
            return definition.Method.DeclaringType.FullName + "." + definition.Method.Name;
        }
    }
}
