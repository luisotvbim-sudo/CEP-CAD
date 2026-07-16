using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PluginConceito.Application.Ribbon
{
    internal sealed class RibbonIconLoader
    {
        private readonly Assembly _assembly;
        private readonly Dictionary<string, ImageSource> _cache =
            new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);

        public RibbonIconLoader(Assembly assembly)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }

        public static bool ResourceExists(Assembly assembly, string resourcePath)
        {
            return FindResourceName(assembly, resourcePath) != null;
        }

        public ImageSource Load(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            ImageSource cached;
            if (_cache.TryGetValue(resourcePath, out cached))
            {
                return cached;
            }

            string resourceName = FindResourceName(_assembly, resourcePath);
            if (resourceName == null)
            {
                return null;
            }

            using (Stream stream = _assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }

                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                _cache[resourcePath] = image;
                return image;
            }
        }

        private static string FindResourceName(Assembly assembly, string resourcePath)
        {
            if (assembly == null || string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            string normalized = resourcePath
                .Replace('\\', '.')
                .Replace('/', '.')
                .Trim('.');

            return assembly.GetManifestResourceNames().FirstOrDefault(name =>
                name.Equals(resourcePath, StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(normalized, StringComparison.OrdinalIgnoreCase));
        }
    }
}
