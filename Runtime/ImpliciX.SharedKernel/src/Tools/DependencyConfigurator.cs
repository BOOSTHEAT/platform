using System;
using ImpliciX.Language.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ImpliciX.SharedKernel.Tools
{
    public class DependencyConfigurator : IConfigureDependency
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly HostBuilderContext _hostingContext;

        public DependencyConfigurator(IServiceCollection serviceCollection, HostBuilderContext hostingContext)
        {
            _serviceCollection = serviceCollection;
            _hostingContext = hostingContext;
        }

        public IConfigureDependency AddSettings<T>(string section, string subSection) where T : class
        {
            Debug.PreCondition(()=>section != null, () => $"{nameof(section)} should not be null");
            Debug.PreCondition(()=>subSection != null, () => $"{nameof(subSection)} should not be null");
            
            var configurationSection = _hostingContext.Configuration.GetSection(section);
             configurationSection = configurationSection.GetSection(subSection);
            _serviceCollection.Configure<T>(subSection, configurationSection);
            return this;
        }
       
        public IConfigureDependency AddSingleton<T>(Func<DependencyConfigurator, T> f) where T : class
        {
            _serviceCollection.AddSingleton<T>(_ => f(this));
            return this;
        }

        public IProvideDependency GetDependencyProvider()
        {
            var serviceProvider = _serviceCollection.BuildServiceProvider();
            return new DependencyProvider(serviceProvider);
        }
    }

    public interface IConfigureDependency
    {
        IConfigureDependency AddSettings<T>(string section, string subSection = null) where T : class;
        IConfigureDependency AddSingleton<T>(Func<DependencyConfigurator, T> f) where T : class;

        IProvideDependency GetDependencyProvider();
    }


    public class NamedService<T>
    {
        public NamedService(string name, T service)
        {
            Name = name;
            Service = service;
        }

        public T Service { get; set; }
        public string Name { get; set; }
    }
}