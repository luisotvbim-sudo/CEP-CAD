using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PluginConceito.Application.Contracts;

namespace PluginConceito.Application.Modules
{
    public sealed class ModuleDiscovery
    {
        public IReadOnlyList<Type> FindModuleTypes(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            return GetLoadableTypes(assembly)
                .Where(type =>
                    typeof(ICntModule).IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    !type.IsInterface)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }
    }
}
