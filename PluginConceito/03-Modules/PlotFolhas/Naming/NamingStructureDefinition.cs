using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PluginConceito.Modules.PlotFolhas
{
    internal sealed class NamingStructureDefinition
    {
        public NamingStructureDefinition(
            string separator,
            IEnumerable<NamingFieldDefinition> fields)
        {
            Separator = separator ?? string.Empty;
            Fields = new ReadOnlyCollection<NamingFieldDefinition>(
                (fields ?? Enumerable.Empty<NamingFieldDefinition>()).ToList());
        }

        public string Separator { get; }

        public IReadOnlyList<NamingFieldDefinition> Fields { get; }

        public RevisionNameTarget RevisionTarget
        {
            get
            {
                NamingFieldDefinition revisionField = Fields.FirstOrDefault(
                    field => field.IsRevision);
                if (revisionField == null ||
                    string.IsNullOrWhiteSpace(revisionField.Value))
                {
                    return RevisionNameTarget.Unspecified;
                }

                int fieldIndex = Fields
                    .TakeWhile(field => !ReferenceEquals(
                        field,
                        revisionField))
                    .Count();
                int segmentIndex = Fields
                    .Take(fieldIndex)
                    .Count(field => !string.IsNullOrWhiteSpace(field.Value));
                return new RevisionNameTarget(
                    segmentIndex,
                    revisionField.Value);
            }
        }
    }

    internal sealed class NamingFieldDefinition
    {
        public NamingFieldDefinition(
            string value,
            bool isSequential,
            bool isRevision)
        {
            Value = value ?? string.Empty;
            IsSequential = isSequential;
            IsRevision = isRevision;
        }

        public string Value { get; }

        public bool IsSequential { get; }

        public bool IsRevision { get; }
    }
}
