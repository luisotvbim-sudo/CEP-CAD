using System;
using System.Collections.Generic;

namespace PluginConceito.Services
{
    public sealed class CntServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void Add<TService>(TService service) where TService : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            _services[typeof(TService)] = service;
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            object service;
            return _services.TryGetValue(serviceType, out service) ? service : null;
        }
    }
}
