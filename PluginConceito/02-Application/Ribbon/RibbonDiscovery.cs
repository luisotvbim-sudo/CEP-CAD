using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PluginConceito.Application.Contracts;
using PluginConceito.Application.Reflection;
using ZwSoft.ZwCAD.Runtime;

namespace PluginConceito.Application.Ribbon
{
    public sealed class RibbonDiscovery
    {
        public IReadOnlyList<RibbonItemDefinition> Discover(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var definitions = new List<RibbonItemDefinition>();

            foreach (Type type in AssemblyTypeLoader
                .GetLoadableTypes(assembly)
                .OrderBy(item => item.FullName, StringComparer.Ordinal))
            {
                MethodInfo[] methods = type.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.Static);

                foreach (MethodInfo method in methods.OrderBy(item => item.Name, StringComparer.Ordinal))
                {
                    var ribbonAttribute = method.GetCustomAttribute<CntRibbonCommandAttribute>(false);
                    if (ribbonAttribute == null)
                    {
                        continue;
                    }

                    var commandAttribute = method.GetCustomAttributes<CommandMethodAttribute>(false).FirstOrDefault();
                    definitions.Add(new RibbonItemDefinition(method, commandAttribute, ribbonAttribute));
                }
            }

            return definitions;
        }
    }
}
