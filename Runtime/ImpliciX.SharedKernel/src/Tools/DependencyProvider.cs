using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ImpliciX.SharedKernel.Tools
{
    public class DependencyProvider : IProvideDependency
    {
        private readonly IServiceProvider _serviceProvider;

        public DependencyProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T GetService<T>() 
            => _serviceProvider.GetService<T>();

        public T GetSettings<T>(string moduleId) where T : class, new() 
            => GetService<IOptionsSnapshot<T>>().Get(moduleId);
    }

    public interface IProvideDependency
    {
        T GetService<T>();

        T GetSettings<T>(string moduleId) where T : class, new(); 
    }
}