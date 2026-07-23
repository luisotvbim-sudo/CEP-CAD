using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PluginConceito.Modules.InsertNotes
{
    internal sealed class DisciplineDefinition
    {
        public DisciplineDefinition(
            string name,
            IEnumerable<DisciplineDefinition> children = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "O nome da disciplina é obrigatório.",
                    nameof(name));
            }

            Name = name;
            Children = new ReadOnlyCollection<DisciplineDefinition>(
                (children ?? Enumerable.Empty<DisciplineDefinition>())
                    .ToList());
        }

        public string Name { get; }

        public IReadOnlyList<DisciplineDefinition> Children { get; }
    }
}
