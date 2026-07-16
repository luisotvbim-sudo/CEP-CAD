using System.Collections.Generic;

namespace PluginConceito.Application.Ribbon
{
    public sealed class RibbonValidationResult
    {
        public RibbonValidationResult(
            IReadOnlyList<RibbonItemDefinition> validDefinitions,
            IReadOnlyList<string> errors)
        {
            ValidDefinitions = validDefinitions;
            Errors = errors;
        }

        public IReadOnlyList<RibbonItemDefinition> ValidDefinitions { get; private set; }

        public IReadOnlyList<string> Errors { get; private set; }
    }
}
