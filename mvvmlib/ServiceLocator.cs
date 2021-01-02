namespace mvvmlib
{
    using System;
    using System.Collections.Generic;

    public sealed class ServiceLocator
    {
        private static readonly Lazy<ServiceLocator> lazyInstance =
            new Lazy<ServiceLocator>(() => new ServiceLocator());

        private readonly Dictionary<Type, object> _registeredServices = new Dictionary<Type, object>();

        public static ServiceLocator Default => lazyInstance.Value;

        private ServiceLocator() { }

        public T GetService<T>() where T : class
        {
            var requestedType = typeof(T);
            if (_registeredServices.ContainsKey(requestedType))
            {
                return (T)_registeredServices[requestedType];
            }
            return default;
        }

        public void RegisterService<T>(T service, bool replace = false) where T : class
        {
            var requestedType = typeof(T);
            if (_registeredServices.ContainsKey(requestedType) && !replace)
            {
                throw new ArgumentException($"A service for type '{requestedType}' is already registered.");
            }
            else
            {
                _registeredServices[requestedType] = service;
            }
        }
    }
}
