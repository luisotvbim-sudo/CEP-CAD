using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PluginConceito.Application.Reflection
{
    internal static class AssemblyTypeLoader
    {
        public static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

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
