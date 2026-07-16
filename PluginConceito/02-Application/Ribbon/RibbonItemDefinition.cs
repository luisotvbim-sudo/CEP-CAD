using System.Reflection;
using PluginConceito.Application.Contracts;
using ZwSoft.ZwCAD.Runtime;

namespace PluginConceito.Application.Ribbon
{
    public sealed class RibbonItemDefinition
    {
        public RibbonItemDefinition(
            MethodInfo method,
            CommandMethodAttribute commandAttribute,
            CntRibbonCommandAttribute ribbonAttribute)
        {
            Method = method;
            CommandAttribute = commandAttribute;
            RibbonAttribute = ribbonAttribute;
        }

        public MethodInfo Method { get; private set; }

        public CommandMethodAttribute CommandAttribute { get; private set; }

        public CntRibbonCommandAttribute RibbonAttribute { get; private set; }
    }
}
